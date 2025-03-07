using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeySim
{
    class calculate
    {
        int interval = 100; //Create 100 steps per simulation
        double initialHeight = 1; //Initial height of the bullet

        public void calculateFlow(double initialHeight, double gravity, double distance, out Point[] bullet, out double[] height)
        {
            try
            {
                // Calculate angle of the bullet
                double angle = Math.Atan(initialHeight / distance);
                height = new double[interval];
                bullet = new Point[interval];

                // Calculate steps of the simulation
                for (int i = 0; i < interval; i++)
                {
                    double time = i / 10.0;
                    double bulletDistanceV = bulletDistance(distance, time, angle);
                    double bulletHeightV = bulletHeight(initialHeight, gravity, time, angle);
                    double monkeyHeightV = monkeyHeight(initialHeight, gravity, time);
                    // Add the bullet and monkey height to the array
                    bullet[i] = new Point((int)bulletDistanceV, (int)bulletHeightV);
                    height[i] = monkeyHeightV;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
                bullet = null;
                height = null;
            }
        }

        private double bulletDistance(double initialVelocity, double time, double angle)
        {
            try
            {
                double distance = initialVelocity * Math.Cos(angle) * time;
                return distance;
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
                return 0;
            }
        }
        private double bulletHeight(double initialHeight, double gravity, double time, double angle)
        {
            try
            {
                double height = initialHeight + Math.Sin(angle) * time - 0.5 * gravity * Math.Pow(time, 2);
                return height;
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
                return 0;
            }
        }
        private double monkeyHeight(double initialHeight, double gravity, double time)
        {
            try
            {
                double height = initialHeight - 0.5 * gravity * Math.Pow(time, 2);
                return height;
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
                return 0;
            }
        }
    }
}
