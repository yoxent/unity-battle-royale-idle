using BattleRoyale.Maps.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class ImageCornerFitTests
    {
        [Test]
        public void DefaultCornersPlaceTheImageOnTheSourceRectangle()
        {
            var upperLeftUv = new Vector2(0f, 1f);
            var lowerRightUv = new Vector2(1f, 0f);
            var floorUpperLeft = new Vector3(-10f, 0f, 20f);
            var floorLowerRight = new Vector3(30f, 0f, -5f);

            var topLeft = ImageCornerFit.ToLocal(
                new Vector2(0f, 1f),
                upperLeftUv,
                lowerRightUv,
                floorUpperLeft,
                floorLowerRight);
            var bottomRight = ImageCornerFit.ToLocal(
                new Vector2(1f, 0f),
                upperLeftUv,
                lowerRightUv,
                floorUpperLeft,
                floorLowerRight);

            Assert.AreEqual(floorUpperLeft, topLeft);
            Assert.AreEqual(floorLowerRight, bottomRight);
        }

        [Test]
        public void InsetCornersExtendTheFullImageOutsideTheSourceRectangle()
        {
            var upperLeftUv = new Vector2(0.25f, 0.75f);
            var lowerRightUv = new Vector2(0.75f, 0.25f);
            var floorUpperLeft = new Vector3(0f, 0f, 10f);
            var floorLowerRight = new Vector3(10f, 0f, 0f);

            var imageTopLeft = ImageCornerFit.ToLocal(
                new Vector2(0f, 1f),
                upperLeftUv,
                lowerRightUv,
                floorUpperLeft,
                floorLowerRight);

            Assert.AreEqual(new Vector3(-5f, 0f, 15f), imageTopLeft);
            Assert.IsFalse(ImageCornerFit.IsValid(lowerRightUv, upperLeftUv));
        }

        [Test]
        public void WorldPositionConvertsBackToTheDraggedImageCoordinate()
        {
            var upperLeftUv = new Vector2(0.2f, 0.8f);
            var lowerRightUv = new Vector2(0.8f, 0.2f);
            var floorUpperLeft = new Vector3(0f, 0f, 8f);
            var floorLowerRight = new Vector3(8f, 0f, 0f);
            var local = new Vector3(4f, 0f, 4f);

            var imageUv = ImageCornerFit.ToImageUv(
                local,
                upperLeftUv,
                lowerRightUv,
                floorUpperLeft,
                floorLowerRight);

            Assert.AreEqual(new Vector2(0.5f, 0.5f), imageUv);
        }

        [Test]
        public void APointOutsideTheFloorKeepsANegativeImageCoordinate()
        {
            var upperLeftUv = new Vector2(0f, 1f);
            var lowerRightUv = new Vector2(1f, 0f);
            var floorUpperLeft = new Vector3(0f, 0f, 10f);
            var floorLowerRight = new Vector3(10f, 0f, 0f);

            var imageUv = ImageCornerFit.ToImageUv(
                new Vector3(-5f, 0f, 15f),
                upperLeftUv,
                lowerRightUv,
                floorUpperLeft,
                floorLowerRight);

            Assert.AreEqual(new Vector2(-0.5f, 1.5f), imageUv);
        }
    }
}
