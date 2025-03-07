using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Xceed.Wpf.Toolkit;


namespace MonkeySim;


/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
        private double monkeyHeight = 5; // Default value in meters
        private double shooterDistance = 10; // Default value in meters
        private double gravity = 9.81; // Default value in m/s²

        // Scaling factors for coordinate system
        private double xScale = 20; // pixels per meter
        private double yScale = 20; // pixels per meter
        private double xMargin = 60;
        private double yMargin = 40;
    private DispatcherTimer animationTimer;
    private double timeElapsed;
    private double initialVelocity;
    private double stoneX, stoneY;
    private double monkeyY;

    public MainWindow()
    {
        InitializeComponent();
        DrawScene();
        InitializeAnimation();
    }

    private void InitializeAnimation()
    {
        animationTimer = new DispatcherTimer();
        animationTimer.Interval = TimeSpan.FromMilliseconds(20);
        animationTimer.Tick += AnimationTimer_Tick;
    }

    private void AnimationTimer_Tick(object sender, EventArgs e)
    {
        timeElapsed += animationTimer.Interval.TotalSeconds;

        // Update stone position
        stoneX = initialVelocity * timeElapsed;
        stoneY = initialVelocity * timeElapsed - 0.5 * gravity * Math.Pow(timeElapsed, 2);

        // Update monkey position
        monkeyY = monkeyHeight - 0.5 * gravity * Math.Pow(timeElapsed, 2);

        // Check for collision
        if (stoneX >= shooterDistance && stoneY <= monkeyY)
        {
            animationTimer.Stop();
            //MessageBox.Show("The stone hit the monkey!");
            return;
        }

        DrawScene(false);
        DrawStone(stoneX, stoneY);
        DrawMonkey(shooterDistance * xScale + xMargin, monkeyY, simulationCanvas.ActualHeight - yMargin, false);
    }

    private void DrawStone(double x, double y)
    {
        Ellipse stone = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = Brushes.Gray
        };
        Canvas.SetLeft(stone, xMargin + x * xScale - 5);
        Canvas.SetTop(stone, simulationCanvas.ActualHeight - yMargin - y * yScale - 5);
        simulationCanvas.Children.Add(stone);
    }

    private void UpdateSimulation_Click(object sender, RoutedEventArgs e)
    {
        // Validate inputs
        if (heightNumericUpDown.Value.HasValue && distanceNumericUpDown.Value.HasValue && gravityNumericUpDown.Value.HasValue)
        {
            double height = heightNumericUpDown.Value.Value;
            double distance = distanceNumericUpDown.Value.Value;
            double gravity = gravityNumericUpDown.Value.Value;

            if (height >= 1 && distance >= 1 && gravity >= 1)
            {
                monkeyHeight = height;
                shooterDistance = distance;
                this.gravity = gravity;

                DrawScene(false); // Draw scene without the monkey
                StartAnimation();
                errorTextBlock.Text = string.Empty;
            }
            else
            {
                errorTextBlock.Text = "Height, distance, and gravity must be at least 1.";
            }
        }
        else
        {
            errorTextBlock.Text = "Please enter valid numbers.";
        }
    }

    private void StartAnimation()
    {
        timeElapsed = 0;
        initialVelocity = shooterDistance / Math.Sqrt(2 * monkeyHeight / gravity);
        stoneX = 0;
        stoneY = 0;
        monkeyY = monkeyHeight;

        animationTimer.Start();
    }

    private void DrawScene(bool drawMonkey = true)
    {
        // Clear the canvas before redrawing
        simulationCanvas.Children.Clear();

        double canvasWidth = simulationCanvas.ActualWidth;
        double canvasHeight = simulationCanvas.ActualHeight;

        if (canvasWidth <= 0 || canvasHeight <= 0)
            return; // Not ready to draw yet

        // Calculate available drawing space
        double availableWidth = canvasWidth - xMargin * 1.5;
        double availableHeight = canvasHeight - yMargin * 1.5;

        // Check if we need to adjust the scale to fit the scene
        bool adjustScale = false;

        // If scene is too big for current scale - halve the scale
        if (shooterDistance * xScale > availableWidth || monkeyHeight * yScale > availableHeight)
        {
            // Determine which scale factor needs more adjustment
            double xRatio = (shooterDistance * xScale) / availableWidth;
            double yRatio = (monkeyHeight * yScale) / availableHeight;

            if (xRatio > yRatio && xRatio > 1.0)
            {
                // Need to halve x scale (repeatedly if needed)
                while (shooterDistance * xScale > availableWidth && xScale > 1.0)
                {
                    xScale /= 2;
                    adjustScale = true;
                }
            }

            if (yRatio > xRatio && yRatio > 1.0)
            {
                // Need to halve y scale (repeatedly if needed)
                while (monkeyHeight * yScale > availableHeight && yScale > 1.0)
                {
                    yScale /= 2;
                    adjustScale = true;
                }
            }
        }
        // If scene is too small (less than half of available space) - double the scale
        else if (shooterDistance * xScale < availableWidth * 0.4 && monkeyHeight * yScale < availableHeight * 0.4)
        {
            // Double the scale but keep x and y scales in sync
            xScale *= 2;
            yScale *= 2;
            adjustScale = true;
        }

        // Ensure minimum scale
        if (xScale < 1.0) xScale = 1.0;
        if (yScale < 1.0) yScale = 1.0;

        // If we adjusted the scale, log it
        if (adjustScale)
        {
            Console.WriteLine($"Adjusted scale to: X={xScale}, Y={yScale}");
        }

        // Calculate the maximum values for our coordinate system based on current scales
        double maxX = Math.Max(shooterDistance * 1.2, availableWidth / xScale);
        double maxY = Math.Max(monkeyHeight * 1.2, availableHeight / yScale);

        // Draw axes
        DrawAxes(canvasWidth, canvasHeight, maxX, maxY);

        // Draw the ground
        Line ground = new Line
        {
            X1 = xMargin,
            Y1 = canvasHeight - yMargin,
            X2 = xMargin + maxX * xScale,
            Y2 = canvasHeight - yMargin,
            Stroke = Brushes.Brown,
            StrokeThickness = 2
        };
        simulationCanvas.Children.Add(ground);

        // Draw the hunter (shooter)
        DrawShooter(xMargin, canvasHeight - yMargin);

        // Draw the monkey if required
        if (drawMonkey)
        {
            DrawMonkey(xMargin + shooterDistance * xScale, monkeyHeight, canvasHeight - yMargin, true);
        }
        else
        {
            double monkeyScale = Math.Min(30, Math.Max(15, Math.Min(xScale, yScale) * 0.75));
            double treeX = xMargin + shooterDistance * xScale + monkeyScale * 0.5;
            DrawTree(treeX, monkeyHeight * yScale, canvasHeight - yMargin);
        }
    }

        private void DrawAxes(double canvasWidth, double canvasHeight, double maxX, double maxY)
        {
            // Y-axis
            Line yAxis = new Line
            {
                X1 = xMargin,
                Y1 = canvasHeight - yMargin,
                X2 = xMargin,
                Y2 = 10,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            simulationCanvas.Children.Add(yAxis);

            // X-axis
            Line xAxis = new Line
            {
                X1 = xMargin,
                Y1 = canvasHeight - yMargin,
                X2 = canvasWidth - 10,
                Y2 = canvasHeight - yMargin,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            simulationCanvas.Children.Add(xAxis);

            // Draw x-axis markers
            int xInterval = DetermineInterval(maxX);
            for (int x = 0; x <= (int)maxX; x += xInterval)
            {
                if (x == 0) continue; // Skip origin

                double xPos = xMargin + x * xScale;

                // Tick mark
                Line tick = new Line
                {
                    X1 = xPos,
                    Y1 = canvasHeight - yMargin,
                    X2 = xPos,
                    Y2 = canvasHeight - yMargin + 5,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                simulationCanvas.Children.Add(tick);

                // Label
                TextBlock label = new TextBlock
                {
                    Text = x.ToString(),
                    FontSize = 10
                };
                Canvas.SetLeft(label, xPos - 5);
                Canvas.SetTop(label, canvasHeight - yMargin + 7);
                simulationCanvas.Children.Add(label);
            }

            // Draw y-axis markers
            int yInterval = DetermineInterval(maxY);
            for (int y = 0; y <= (int)maxY; y += yInterval)
            {
                if (y == 0) continue; // Skip origin

                double yPos = canvasHeight - yMargin - y * yScale;

                // Tick mark
                Line tick = new Line
                {
                    X1 = xMargin,
                    Y1 = yPos,
                    X2 = xMargin - 5,
                    Y2 = yPos,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                simulationCanvas.Children.Add(tick);

                // Label
                TextBlock label = new TextBlock
                {
                    Text = y.ToString(),
                    FontSize = 10
                };
                Canvas.SetLeft(label, xMargin - 25);
                Canvas.SetTop(label, yPos - 7);
                simulationCanvas.Children.Add(label);
            }

            // Axis labels
            TextBlock xAxisLabel = new TextBlock
            {
                Text = "Vzdálenost (m)",
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(xAxisLabel, canvasWidth / 2);
            Canvas.SetTop(xAxisLabel, canvasHeight - 20);
            simulationCanvas.Children.Add(xAxisLabel);

            TextBlock yAxisLabel = new TextBlock
            {
                Text = "Výška (m)",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                RenderTransform = new RotateTransform(-90)
            };
            Canvas.SetLeft(yAxisLabel, 10);
            Canvas.SetTop(yAxisLabel, canvasHeight / 2);
            simulationCanvas.Children.Add(yAxisLabel);
        }

        private int DetermineInterval(double maxValue)
        {
            if (maxValue <= 10) return 1;
            if (maxValue <= 20) return 2;
            if (maxValue <= 50) return 5;
            if (maxValue <= 100) return 10;
            if (maxValue <= 500) return 50;
            if (maxValue <= 1000) return 100;
            if (maxValue <= 5000) return 250;
            return 1000;
        }

        //private void UpdateSimulation_Click(object sender, RoutedEventArgs e)
        //{
        //    // Validate inputs
        //    if (heightNumericUpDown.Value.HasValue && distanceNumericUpDown.Value.HasValue && gravityNumericUpDown.Value.HasValue)
        //    {
        //        double height = heightNumericUpDown.Value.Value;
        //        double distance = distanceNumericUpDown.Value.Value;
        //        double gravity = gravityNumericUpDown.Value.Value;

        //        if (height >= 1 && distance >= 1 && gravity >= 1)
        //        {
        //            monkeyHeight = height;
        //            shooterDistance = distance;
        //            this.gravity = gravity;

        //            DrawScene();
        //            DrawSimulation();
        //            errorTextBlock.Text = string.Empty;
        //        }
        //        else
        //        {
        //            errorTextBlock.Text = "Height, distance, and gravity must be at least 1.";
        //        }
        //    }
        //    else
        //    {
        //        errorTextBlock.Text = "Please enter valid numbers.";
        //    }
        //}

        private void DrawSimulation()
        {
            double height = heightNumericUpDown.Value ?? 0;
            double distance = distanceNumericUpDown.Value ?? 0;
            double gravity = gravityNumericUpDown.Value ?? 0;

            // Create an instance of the calculate class
            var calculator = new calculate();

            // Calculate the trajectory
            calculator.calculateFlow(height, gravity, distance, out System.Drawing.Point[] bullet, out double[] monkeyHeights);

            // Draw the bullet trajectory
            //for (int i = 0; i < bullet.Length - 1; i++)
            //{
            //    Line bulletTrajectory = new Line
            //    {
            //        X1 = xMargin + bullet[i].X * xScale,
            //        Y1 = yMargin + bullet[i].Y * yScale,
            //        X2 = xMargin + bullet[i + 1].X * xScale,
            //        Y2 = yMargin + bullet[i + 1].Y * yScale,
            //        Stroke = Brushes.Black,
            //        StrokeThickness = 2
            //    };
            //    simulationCanvas.Children.Add(bulletTrajectory);
            //}

            // Draw the monkey's path
            for (int i = 0; i < monkeyHeights.Length - 1; i++)
            {
                Line monkeyPath = new Line
                {
                    X1 = xMargin + distance * xScale,
                    Y1 = simulationCanvas.ActualHeight - yMargin - monkeyHeights[i] * yScale,
                    X2 = xMargin + distance * xScale,
                    Y2 = simulationCanvas.ActualHeight - yMargin - monkeyHeights[i + 1] * yScale,
                    Stroke = Brushes.Brown,
                    StrokeThickness = 2
                };
                simulationCanvas.Children.Add(monkeyPath);
            }
        }

    private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            DrawScene();
    }
    /// <summary>
    /// Draw the shooter (hunter) on the canvas
    /// </summary>
    /// <param name="x">Where to start on X axis</param>
    /// <param name="groundY">Where to start on Y axis</param>
    private void DrawShooter(double x, double groundY)
    {
        double shooterHeight = Math.Min(80, Math.Max(30, yScale * 2));
        double headSize = shooterHeight * 0.25;
        double bodyWidth = shooterHeight * 0.4;

        // Head
        Ellipse head = new Ellipse
        {
            Width = headSize,
            Height = headSize,
            Fill = Brushes.Bisque,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };
        Canvas.SetLeft(head, x - headSize / 2);
        Canvas.SetTop(head, groundY - shooterHeight);
        simulationCanvas.Children.Add(head);

        // Eyes
        double eyeSize = headSize * 0.2;
        Ellipse leftEye = new Ellipse
        {
            Width = eyeSize,
            Height = eyeSize,
            Fill = Brushes.White,
            Stroke = Brushes.Black,
            StrokeThickness = 0.5
        };
        Canvas.SetLeft(leftEye, x - headSize * 0.25);
        Canvas.SetTop(leftEye, groundY - shooterHeight + headSize * 0.3);
        simulationCanvas.Children.Add(leftEye);

        Ellipse rightEye = new Ellipse
        {
            Width = eyeSize,
            Height = eyeSize,
            Fill = Brushes.White,
            Stroke = Brushes.Black,
            StrokeThickness = 0.5
        };
        Canvas.SetLeft(rightEye, x + headSize * 0.05);
        Canvas.SetTop(rightEye, groundY - shooterHeight + headSize * 0.3);
        simulationCanvas.Children.Add(rightEye);

        // Pupils
        double pupilSize = eyeSize * 0.6;
        Ellipse leftPupil = new Ellipse
        {
            Width = pupilSize,
            Height = pupilSize,
            Fill = Brushes.Black
        };
        Canvas.SetLeft(leftPupil, x - headSize * 0.25 + (eyeSize - pupilSize) / 2);
        Canvas.SetTop(leftPupil, groundY - shooterHeight + headSize * 0.3 + (eyeSize - pupilSize) / 2);
        simulationCanvas.Children.Add(leftPupil);

        Ellipse rightPupil = new Ellipse
        {
            Width = pupilSize,
            Height = pupilSize,
            Fill = Brushes.Black
        };
        Canvas.SetLeft(rightPupil, x + headSize * 0.05 + (eyeSize - pupilSize) / 2);
        Canvas.SetTop(rightPupil, groundY - shooterHeight + headSize * 0.3 + (eyeSize - pupilSize) / 2);
        simulationCanvas.Children.Add(rightPupil);

        // Body
        Rectangle body = new Rectangle
        {
            Width = bodyWidth,
            Height = shooterHeight * 0.5,
            Fill = Brushes.DarkGreen,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };
        Canvas.SetLeft(body, x - bodyWidth / 2);
        Canvas.SetTop(body, groundY - shooterHeight + headSize);
        simulationCanvas.Children.Add(body);

        // Legs
        double legWidth = bodyWidth * 0.3;
        double legHeight = shooterHeight * 0.25;

        Rectangle leftLeg = new Rectangle
        {
            Width = legWidth,
            Height = legHeight,
            Fill = Brushes.DarkOliveGreen,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };
        Canvas.SetLeft(leftLeg, x - bodyWidth * 0.4);
        Canvas.SetTop(leftLeg, groundY - legHeight);
        simulationCanvas.Children.Add(leftLeg);

        Rectangle rightLeg = new Rectangle
        {
            Width = legWidth,
            Height = legHeight,
            Fill = Brushes.DarkOliveGreen,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };
        Canvas.SetLeft(rightLeg, x + bodyWidth * 0.1);
        Canvas.SetTop(rightLeg, groundY - legHeight);
        simulationCanvas.Children.Add(rightLeg);

        // Arms
        double armWidth = bodyWidth * 0.7;
        double armHeight = bodyWidth * 0.25;

        // Left arm
        Rectangle leftArm = new Rectangle
        {
            Width = armWidth * 0.6,
            Height = armHeight,
            Fill = Brushes.DarkGreen,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };
        Canvas.SetLeft(leftArm, x - bodyWidth * 0.5 - armWidth * 0.3);
        Canvas.SetTop(leftArm, groundY - shooterHeight + headSize + body.Height * 0.2);
        simulationCanvas.Children.Add(leftArm);

        // Right arm (holding gun)
        Rectangle rightArm = new Rectangle
        {
            Width = armWidth * 0.6,
            Height = armHeight,
            Fill = Brushes.DarkGreen,
            Stroke = Brushes.Black,
            StrokeThickness = 1,
            RenderTransform = new RotateTransform(15)
        };
        Canvas.SetLeft(rightArm, x + bodyWidth * 0.3);
        Canvas.SetTop(rightArm, groundY - shooterHeight + headSize + body.Height * 0.2);
        simulationCanvas.Children.Add(rightArm);

        // Gun
        Rectangle gunBase = new Rectangle
        {
            Width = armWidth * 0.9,
            Height = armHeight * 0.6,
            Fill = Brushes.Black,
            Stroke = Brushes.DarkGray,
            StrokeThickness = 1,
            RenderTransform = new RotateTransform(15)
        };
        Canvas.SetLeft(gunBase, x + bodyWidth * 0.6);
        Canvas.SetTop(gunBase, groundY - shooterHeight + headSize + body.Height * 0.2);
        simulationCanvas.Children.Add(gunBase);

        Rectangle gunBarrel = new Rectangle
        {
            Width = armWidth * 1.2,
            Height = armHeight * 0.3,
            Fill = Brushes.DimGray,
            Stroke = Brushes.Black,
            StrokeThickness = 0.5,
            RenderTransform = new RotateTransform(15)
        };
        Canvas.SetLeft(gunBarrel, x + bodyWidth * 0.95);
        Canvas.SetTop(gunBarrel, groundY - shooterHeight + headSize + body.Height * 0.25);
        simulationCanvas.Children.Add(gunBarrel);

        // Hat
        Path hat = new Path
        {
            Fill = Brushes.DarkOliveGreen,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };

        PathGeometry hatGeometry = new PathGeometry();
        PathFigure hatFigure = new PathFigure();

        hatFigure.StartPoint = new Point(x - headSize * 0.7, groundY - shooterHeight + headSize * 0.1);

        hatFigure.Segments.Add(new LineSegment(new Point(x + headSize * 0.7, groundY - shooterHeight + headSize * 0.1), true));
        hatFigure.Segments.Add(new LineSegment(new Point(x + headSize * 0.4, groundY - shooterHeight - headSize * 0.3), true));
        hatFigure.Segments.Add(new LineSegment(new Point(x - headSize * 0.4, groundY - shooterHeight - headSize * 0.3), true));
        hatFigure.Segments.Add(new LineSegment(new Point(x - headSize * 0.7, groundY - shooterHeight + headSize * 0.1), true));

        hatFigure.IsClosed = true;
        hatGeometry.Figures.Add(hatFigure);
        hat.Data = hatGeometry;

        simulationCanvas.Children.Add(hat);
    }
    private void DrawMonkey(double x, double treeHeight, double groundY, bool drawTree)
    {
        double monkeyScale = Math.Min(30, Math.Max(15, Math.Min(xScale, yScale) * 0.75));

        if (drawTree)
        {
            // Position tree behind the monkey - move it a bit to the right
            double treeX = x + monkeyScale * 0.5;
            DrawTree(treeX, treeHeight * yScale, groundY);
        }

        // Calculate branch position
        double branchY = groundY - treeHeight * yScale * 0.85;
        double branchThickness = Math.Min(40, Math.Max(20, treeHeight / 8)) * 0.35;
        double trunkWidth = Math.Min(40, Math.Max(20, treeHeight * yScale / 8));
        double branchWidth = trunkWidth * 2.5;

        // Position monkey at the exact position specified (x) - now monkey is in front of tree
        // but appears to hang from the branch
        double monkeyX = x;
        double monkeyY = branchY + branchThickness;

        // Monkey body
        double bodyWidth = monkeyScale * 0.8;
        double bodyHeight = monkeyScale * 1.2;

        Ellipse body = new Ellipse
        {
            Width = bodyWidth,
            Height = bodyHeight,
            Fill = new SolidColorBrush(Color.FromRgb(139, 69, 19)), // SaddleBrown
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };
        Canvas.SetLeft(body, monkeyX - bodyWidth / 2);
        Canvas.SetTop(body, monkeyY); // Position below branch
        simulationCanvas.Children.Add(body);

        // Monkey head
        double headSize = monkeyScale * 0.9;

        Ellipse head = new Ellipse
        {
            Width = headSize,
            Height = headSize,
            Fill = new SolidColorBrush(Color.FromRgb(160, 82, 45)), // Sienna 
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };
        Canvas.SetLeft(head, monkeyX - headSize / 2);
        Canvas.SetTop(head, monkeyY - headSize * 0.8);
        simulationCanvas.Children.Add(head);

        // Face - adjust position relative to head
        Ellipse face = new Ellipse
        {
            Width = headSize * 0.7,
            Height = headSize * 0.6,
            Fill = Brushes.Bisque,
            Stroke = Brushes.Black,
            StrokeThickness = 0.5
        };
        Canvas.SetLeft(face, monkeyX - headSize * 0.35);
        Canvas.SetTop(face, monkeyY - headSize * 0.7);
        simulationCanvas.Children.Add(face);

        // Eyes - position relative to face
        double eyeSize = headSize * 0.15;

        // Left eye
        Ellipse leftEye = new Ellipse
        {
            Width = eyeSize,
            Height = eyeSize,
            Fill = Brushes.White,
            Stroke = Brushes.Black,
            StrokeThickness = 0.5
        };
        Canvas.SetLeft(leftEye, monkeyX - headSize * 0.25);
        Canvas.SetTop(leftEye, monkeyY - headSize * 0.65);
        simulationCanvas.Children.Add(leftEye);

        // Left pupil
        Ellipse leftPupil = new Ellipse
        {
            Width = eyeSize * 0.6,
            Height = eyeSize * 0.6,
            Fill = Brushes.Black
        };
        Canvas.SetLeft(leftPupil, monkeyX - headSize * 0.25 + eyeSize * 0.2);
        Canvas.SetTop(leftPupil, monkeyY - headSize * 0.65 + eyeSize * 0.2);
        simulationCanvas.Children.Add(leftPupil);

        // Right eye
        Ellipse rightEye = new Ellipse
        {
            Width = eyeSize,
            Height = eyeSize,
            Fill = Brushes.White,
            Stroke = Brushes.Black,
            StrokeThickness = 0.5
        };
        Canvas.SetLeft(rightEye, monkeyX + headSize * 0.1);
        Canvas.SetTop(rightEye, monkeyY - headSize * 0.65);
        simulationCanvas.Children.Add(rightEye);

        // Right pupil
        Ellipse rightPupil = new Ellipse
        {
            Width = eyeSize * 0.6,
            Height = eyeSize * 0.6,
            Fill = Brushes.Black
        };
        Canvas.SetLeft(rightPupil, monkeyX + headSize * 0.1 + eyeSize * 0.2);
        Canvas.SetTop(rightPupil, monkeyY - headSize * 0.65 + eyeSize * 0.2);
        simulationCanvas.Children.Add(rightPupil);

        // Smile
        Path mouth = new Path
        {
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };

        PathGeometry mouthGeometry = new PathGeometry();
        PathFigure mouthFigure = new PathFigure();

        mouthFigure.StartPoint = new Point(monkeyX - headSize * 0.15, monkeyY - headSize * 0.45);
        ArcSegment arc = new ArcSegment
        {
            Point = new Point(monkeyX + headSize * 0.15, monkeyY - headSize * 0.45),
            Size = new Size(headSize * 0.2, headSize * 0.1),
            SweepDirection = SweepDirection.Clockwise
        };
        mouthFigure.Segments.Add(arc);
        mouthGeometry.Figures.Add(mouthFigure);
        mouth.Data = mouthGeometry;
        simulationCanvas.Children.Add(mouth);

        // Ears - position relative to head
        double earSize = headSize * 0.25;

        // Left ear
        Ellipse leftEar = new Ellipse
        {
            Width = earSize,
            Height = earSize,
            Fill = new SolidColorBrush(Color.FromRgb(139, 69, 19)),
            Stroke = Brushes.Black,
            StrokeThickness = 0.5
        };
        Canvas.SetLeft(leftEar, monkeyX - headSize * 0.5 - earSize * 0.3);
        Canvas.SetTop(leftEar, monkeyY - headSize * 0.7);
        simulationCanvas.Children.Add(leftEar);

        // Right ear
        Ellipse rightEar = new Ellipse
        {
            Width = earSize,
            Height = earSize,
            Fill = new SolidColorBrush(Color.FromRgb(139, 69, 19)),
            Stroke = Brushes.Black,
            StrokeThickness = 0.5
        };
        Canvas.SetLeft(rightEar, monkeyX + headSize * 0.5 - earSize * 0.7);
        Canvas.SetTop(rightEar, monkeyY - headSize * 0.7);
        simulationCanvas.Children.Add(rightEar);

        // Arms positioned to hang onto branch - extending toward tree
        double armWidth = bodyWidth * 0.25;

        // Left arm reaching up to branch
        Path leftArm = new Path
        {
            Fill = new SolidColorBrush(Color.FromRgb(139, 69, 19)),
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };

        PathGeometry leftArmGeometry = new PathGeometry();
        PathFigure leftArmFigure = new PathFigure();

        leftArmFigure.StartPoint = new Point(monkeyX - bodyWidth * 0.3, monkeyY + bodyHeight * 0.2);
        leftArmFigure.Segments.Add(new BezierSegment(
            new Point(monkeyX - bodyWidth * 0.5, monkeyY),
            new Point(monkeyX - bodyWidth * 0.2, branchY + branchThickness),
            new Point(monkeyX - bodyWidth * 0.1, branchY + branchThickness / 2),
            true));
        leftArmFigure.Segments.Add(new BezierSegment(
            new Point(monkeyX, branchY + branchThickness * 0.8),
            new Point(monkeyX - bodyWidth * 0.1, monkeyY + bodyHeight * 0.1),
            new Point(monkeyX - bodyWidth * 0.1, monkeyY + bodyHeight * 0.2),
            true));

        leftArmFigure.IsClosed = true;
        leftArmGeometry.Figures.Add(leftArmFigure);
        leftArm.Data = leftArmGeometry;
        simulationCanvas.Children.Add(leftArm);

        // Right arm reaching up to branch - extending toward tree
        Path rightArm = new Path
        {
            Fill = new SolidColorBrush(Color.FromRgb(139, 69, 19)),
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };

        PathGeometry rightArmGeometry = new PathGeometry();
        PathFigure rightArmFigure = new PathFigure();

        rightArmFigure.StartPoint = new Point(monkeyX + bodyWidth * 0.3, monkeyY + bodyHeight * 0.2);
        rightArmFigure.Segments.Add(new BezierSegment(
            new Point(monkeyX + bodyWidth * 0.5, monkeyY),
            new Point(monkeyX + bodyWidth * 0.6, branchY + branchThickness),
            new Point(monkeyX + bodyWidth * 0.5, branchY + branchThickness / 2),
            true));
        rightArmFigure.Segments.Add(new BezierSegment(
            new Point(monkeyX + bodyWidth * 0.4, branchY + branchThickness * 0.8),
            new Point(monkeyX + bodyWidth * 0.2, monkeyY + bodyHeight * 0.1),
            new Point(monkeyX + bodyWidth * 0.1, monkeyY + bodyHeight * 0.2),
            true));

        rightArmFigure.IsClosed = true;
        rightArmGeometry.Figures.Add(rightArmFigure);
        rightArm.Data = rightArmGeometry;
        simulationCanvas.Children.Add(rightArm);

        // Legs hanging down
        double legWidth = bodyWidth * 0.25;
        double legLength = bodyHeight * 0.7;

        Rectangle leftLeg = new Rectangle
        {
            Width = legWidth,
            Height = legLength,
            Fill = new SolidColorBrush(Color.FromRgb(139, 69, 19)),
            Stroke = Brushes.Black,
            StrokeThickness = 1,
            RadiusX = legWidth / 2,
            RadiusY = legWidth / 2,
            RenderTransform = new RotateTransform(15)
        };
        Canvas.SetLeft(leftLeg, monkeyX - bodyWidth * 0.4);
        Canvas.SetTop(leftLeg, monkeyY + bodyHeight * 0.8);
        simulationCanvas.Children.Add(leftLeg);

        // Right leg
        Rectangle rightLeg = new Rectangle
        {
            Width = legWidth,
            Height = legLength,
            Fill = new SolidColorBrush(Color.FromRgb(139, 69, 19)),
            Stroke = Brushes.Black,
            StrokeThickness = 1,
            RadiusX = legWidth / 2,
            RadiusY = legWidth / 2,
            RenderTransform = new RotateTransform(-15)
        };
        Canvas.SetLeft(rightLeg, monkeyX + bodyWidth * 0.15);
        Canvas.SetTop(rightLeg, monkeyY + bodyHeight * 0.8);
        simulationCanvas.Children.Add(rightLeg);

        // Tail
        Path tail = new Path
        {
            Stroke = new SolidColorBrush(Color.FromRgb(139, 69, 19)),
            StrokeThickness = armWidth * 0.8,
            StrokeEndLineCap = PenLineCap.Round
        };

        PathGeometry tailGeometry = new PathGeometry();
        PathFigure tailFigure = new PathFigure();

        tailFigure.StartPoint = new Point(monkeyX, monkeyY + bodyHeight * 0.8);

        BezierSegment bezier = new BezierSegment
        {
            Point1 = new Point(monkeyX + bodyWidth * 0.5, monkeyY + bodyHeight * 1.1),
            Point2 = new Point(monkeyX + bodyWidth * 0.8, monkeyY + bodyHeight * 0.9),
            Point3 = new Point(monkeyX + bodyWidth * 0.9, monkeyY + bodyHeight * 0.5)
        };

        tailFigure.Segments.Add(bezier);
        tailGeometry.Figures.Add(tailFigure);
        tail.Data = tailGeometry;
        simulationCanvas.Children.Add(tail);
    }
    /// <summary>
    /// Draw a tree at the specified location with the given height
    /// </summary>
    /// <param name="x">The x-coordinate for the tree</param>
    /// <param name="treeHeight">The height of the tree in scaled units</param>
    /// <param name="groundY">The y-coordinate of the ground</param>
    private void DrawTree(double x, double treeHeight, double groundY)
    {
        // Tree dimensions
        double trunkWidth = Math.Min(40, Math.Max(20, treeHeight / 8));
        double trunkHeight = treeHeight;

        // Create a linear gradient brush for trunk with bark-like texture
        LinearGradientBrush trunkBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 0)
        };
        trunkBrush.GradientStops.Add(new GradientStop(Color.FromRgb(90, 57, 23), 0.0));
        trunkBrush.GradientStops.Add(new GradientStop(Color.FromRgb(115, 77, 38), 0.4));
        trunkBrush.GradientStops.Add(new GradientStop(Color.FromRgb(101, 67, 33), 0.6));
        trunkBrush.GradientStops.Add(new GradientStop(Color.FromRgb(85, 57, 23), 1.0));

        // Draw trunk with texture
        Path trunk = new Path
        {
            Fill = trunkBrush,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };

        PathGeometry trunkGeometry = new PathGeometry();
        PathFigure trunkFigure = new PathFigure();

        // Create a slightly curved trunk for more realism
        double trunkCurve = trunkWidth * 0.15;

        trunkFigure.StartPoint = new Point(x - trunkWidth / 2, groundY);
        trunkFigure.Segments.Add(new LineSegment(new Point(x - trunkWidth / 2, groundY - trunkHeight * 0.7), true));
        trunkFigure.Segments.Add(new BezierSegment(
            new Point(x - trunkWidth / 2 + trunkCurve, groundY - trunkHeight * 0.85),
            new Point(x - trunkWidth / 3, groundY - trunkHeight * 0.95),
            new Point(x - trunkWidth / 4, groundY - trunkHeight), true));
        trunkFigure.Segments.Add(new LineSegment(new Point(x + trunkWidth / 4, groundY - trunkHeight), true));
        trunkFigure.Segments.Add(new BezierSegment(
            new Point(x + trunkWidth / 3, groundY - trunkHeight * 0.95),
            new Point(x + trunkWidth / 2 - trunkCurve, groundY - trunkHeight * 0.85),
            new Point(x + trunkWidth / 2, groundY - trunkHeight * 0.7), true));
        trunkFigure.Segments.Add(new LineSegment(new Point(x + trunkWidth / 2, groundY), true));

        trunkFigure.IsClosed = true;
        trunkGeometry.Figures.Add(trunkFigure);
        trunk.Data = trunkGeometry;
        simulationCanvas.Children.Add(trunk);

        // Add trunk texture details - bark lines
        for (int i = 0; i < 5; i++)
        {
            double yPos = groundY - trunkHeight * (0.2 + 0.15 * i);
            double length = trunkWidth * (0.3 + 0.1 * (i % 3));
            double xOffset = (i % 2 == 0) ? -length / 2 : -length / 3;

            Line barkLine = new Line
            {
                X1 = x + xOffset,
                Y1 = yPos,
                X2 = x + xOffset + length,
                Y2 = yPos,
                Stroke = new SolidColorBrush(Color.FromRgb(75, 47, 20)),
                StrokeThickness = 1.5
            };
            simulationCanvas.Children.Add(barkLine);
        }

        // Draw branches
        double branchY = groundY - trunkHeight * 0.85;
        double branchWidth = trunkWidth * 2.5;
        double branchThickness = trunkWidth * 0.35;

        // Branch brush with gradient
        LinearGradientBrush branchBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        branchBrush.GradientStops.Add(new GradientStop(Color.FromRgb(101, 67, 33), 0.0));
        branchBrush.GradientStops.Add(new GradientStop(Color.FromRgb(115, 77, 38), 0.5));
        branchBrush.GradientStops.Add(new GradientStop(Color.FromRgb(90, 57, 23), 1.0));

        // Main branch extending to the left (toward shooter)
        Path mainBranch = new Path
        {
            Fill = branchBrush,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };

        PathGeometry mainBranchGeometry = new PathGeometry();
        PathFigure mainBranchFigure = new PathFigure();

        // Create a more natural curved branch
        double branchCurve = branchThickness * 0.8;

        mainBranchFigure.StartPoint = new Point(x - trunkWidth * 0.3, branchY);
        mainBranchFigure.Segments.Add(new BezierSegment(
            new Point(x - branchWidth * 0.4, branchY - branchCurve),
            new Point(x - branchWidth * 0.7, branchY - branchCurve * 0.5),
            new Point(x - branchWidth, branchY + branchThickness * 0.2), true));
        mainBranchFigure.Segments.Add(new LineSegment(new Point(x - branchWidth, branchY + branchThickness), true));
        mainBranchFigure.Segments.Add(new BezierSegment(
            new Point(x - branchWidth * 0.7, branchY + branchThickness + branchCurve * 0.2),
            new Point(x - branchWidth * 0.4, branchY + branchThickness + branchCurve * 0.1),
            new Point(x - trunkWidth * 0.1, branchY + branchThickness), true));

        mainBranchFigure.IsClosed = true;
        mainBranchGeometry.Figures.Add(mainBranchFigure);
        mainBranch.Data = mainBranchGeometry;
        simulationCanvas.Children.Add(mainBranch);

        // Small right branch with more natural curve
        Path rightBranch = new Path
        {
            Fill = branchBrush,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };

        PathGeometry rightGeometry = new PathGeometry();
        PathFigure rightFigure = new PathFigure();

        rightFigure.StartPoint = new Point(x + trunkWidth * 0.1, branchY + branchThickness / 2);
        rightFigure.Segments.Add(new BezierSegment(
            new Point(x + trunkWidth * 0.5, branchY - branchThickness * 0.3),
            new Point(x + trunkWidth, branchY - branchThickness * 0.5),
            new Point(x + trunkWidth * 1.5, branchY), true));
        rightFigure.Segments.Add(new LineSegment(new Point(x + trunkWidth * 1.5, branchY + branchThickness * 0.5), true));
        rightFigure.Segments.Add(new BezierSegment(
            new Point(x + trunkWidth, branchY + branchThickness * 0.1),
            new Point(x + trunkWidth * 0.5, branchY + branchThickness * 0.3),
            new Point(x + trunkWidth * 0.1, branchY + branchThickness), true));

        rightFigure.IsClosed = true;
        rightGeometry.Figures.Add(rightFigure);
        rightBranch.Data = rightGeometry;
        simulationCanvas.Children.Add(rightBranch);

        // Enhanced foliage with multiple layers and better coloring
        double foliageRadius = trunkWidth * 1.8;

        // Create a radial gradient for foliage
        RadialGradientBrush foliageBrush = new RadialGradientBrush
        {
            GradientOrigin = new Point(0.5, 0.3),
            Center = new Point(0.5, 0.5),
            RadiusX = 0.5,
            RadiusY = 0.5
        };
        foliageBrush.GradientStops.Add(new GradientStop(Color.FromRgb(65, 160, 65), 0.0));
        foliageBrush.GradientStops.Add(new GradientStop(Color.FromRgb(34, 139, 34), 0.7));
        foliageBrush.GradientStops.Add(new GradientStop(Color.FromRgb(25, 100, 25), 1.0));

        // Multiple clusters of leaves with varying sizes for volume
        Random rand = new Random();
        for (int i = 0; i < 5; i++)
        {
            double offsetX = (rand.NextDouble() * 2 - 1) * foliageRadius * 0.6;
            double offsetY = (rand.NextDouble() * -1) * foliageRadius * 0.8;
            double sizeVar = 0.8 + rand.NextDouble() * 0.4;

            Ellipse foliage = new Ellipse
            {
                Width = foliageRadius * 2 * sizeVar,
                Height = foliageRadius * 1.8 * sizeVar,
                Fill = foliageBrush,
                Stroke = new SolidColorBrush(Color.FromRgb(25, 100, 25)),
                StrokeThickness = 1
            };
            Canvas.SetLeft(foliage, x - foliageRadius * sizeVar + offsetX);
            Canvas.SetTop(foliage, groundY - trunkHeight - foliageRadius * 0.8 + offsetY);
            simulationCanvas.Children.Add(foliage);
        }

        // Enhanced branch foliage
        // Left branch foliage
        double smallFoliageRadius = foliageRadius * 0.6;
        RadialGradientBrush smallFoliageBrush = new RadialGradientBrush();
        smallFoliageBrush.GradientStops.Add(new GradientStop(Color.FromRgb(65, 160, 65), 0.0));
        smallFoliageBrush.GradientStops.Add(new GradientStop(Color.FromRgb(34, 139, 34), 0.8));

        // Create multiple smaller foliage clusters around the left branch
        for (int i = 0; i < 2; i++)
        {
            double offsetX = (i == 0) ? -smallFoliageRadius * 0.5 : -smallFoliageRadius * 1.2;
            double offsetY = (i == 0) ? -smallFoliageRadius * 0.3 : -smallFoliageRadius * 0.8;

            Ellipse leftFoliage = new Ellipse
            {
                Width = smallFoliageRadius * 2 * (0.9 + rand.NextDouble() * 0.2),
                Height = smallFoliageRadius * 1.8 * (0.9 + rand.NextDouble() * 0.2),
                Fill = smallFoliageBrush,
                Stroke = Brushes.DarkGreen,
                StrokeThickness = 1
            };
            Canvas.SetLeft(leftFoliage, x - branchWidth * 0.8 + offsetX);
            Canvas.SetTop(leftFoliage, branchY - smallFoliageRadius + offsetY);
            simulationCanvas.Children.Add(leftFoliage);
        }

        // Right branch foliage clusters
        for (int i = 0; i < 2; i++)
        {
            double offsetX = (i == 0) ? smallFoliageRadius * 0.2 : smallFoliageRadius * 0.8;
            double offsetY = (i == 0) ? -smallFoliageRadius * 0.5 : -smallFoliageRadius * 0.2;

            Ellipse rightFoliage = new Ellipse
            {
                Width = smallFoliageRadius * (1.4 + rand.NextDouble() * 0.3),
                Height = smallFoliageRadius * (1.2 + rand.NextDouble() * 0.3),
                Fill = smallFoliageBrush,
                Stroke = Brushes.DarkGreen,
                StrokeThickness = 1
            };
            Canvas.SetLeft(rightFoliage, x + trunkWidth - smallFoliageRadius * 0.5 + offsetX);
            Canvas.SetTop(rightFoliage, branchY - smallFoliageRadius * 0.8 + offsetY);
            simulationCanvas.Children.Add(rightFoliage);
        }

        // Add some ground detail around the base of the tree
        for (int i = 0; i < 5; i++)
        {
            double grassWidth = trunkWidth * (0.3 + rand.NextDouble() * 0.3);
            double grassHeight = trunkWidth * (0.2 + rand.NextDouble() * 0.2);
            double offsetX = trunkWidth * (rand.NextDouble() * 1.2 - 0.6);

            Ellipse grass = new Ellipse
            {
                Width = grassWidth,
                Height = grassHeight,
                Fill = new SolidColorBrush(Color.FromRgb(60, 150, 60)),
                Stroke = Brushes.DarkGreen,
                StrokeThickness = 0.5
            };
            Canvas.SetLeft(grass, x - grassWidth / 2 + offsetX);
            Canvas.SetTop(grass, groundY - grassHeight * 0.8);
            simulationCanvas.Children.Add(grass);
        }
    }
    private void HeightNumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (heightNumericUpDown.Value.HasValue)
        {
            monkeyHeight = heightNumericUpDown.Value.Value;
            DrawScene();
        }
    }

    private void DistanceNumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (distanceNumericUpDown.Value.HasValue)
        {
            shooterDistance = distanceNumericUpDown.Value.Value;
            DrawScene();
        }
    }
}
