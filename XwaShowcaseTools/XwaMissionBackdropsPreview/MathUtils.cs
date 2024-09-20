using System;

namespace XwaMissionBackdropsPreview;

internal static class MathUtils
{
    public static void ComputeHeadingAngles(int positionX, int positionY, int positionZ, out double headingXY, out double headingZ)
    {
        if (positionX == 0 && positionY == 0)
        {
            headingXY = 0.0;
        }
        else if (positionX == 0)
        {
            if (positionY > 0)
            {
                headingXY = 0.0;
            }
            else
            {
                headingXY = Math.PI;
            }
        }
        else
        {
            double posX = positionX;
            double posY = positionY;
            double length = Math.Sqrt(posX * posX + posY * posY);
            posY /= length;

            if (positionX > 0)
            {
                headingXY = Math.Acos(posY);
            }
            else
            {
                headingXY = -Math.Acos(posY);
            }
        }

        headingXY -= Math.PI / 2;

        if (positionZ == 0)
        {
            headingZ = 0.0;
        }
        else if (positionX == 0 && positionY == 0)
        {
            if (positionZ < 0)
            {
                headingZ = Math.PI / 2;
            }
            else
            {
                headingZ = -Math.PI / 2;
            }
        }
        else
        {
            double posX = positionX;
            double posY = positionY;
            double posZ = positionZ;
            double length = Math.Sqrt(posX * posX + posY * posY + posZ * posZ);
            posZ /= length;

            headingZ = Math.Asin(posZ);
        }
    }
}
