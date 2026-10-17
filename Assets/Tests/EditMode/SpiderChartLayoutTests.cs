using BattleRoyale.Company;
using NUnit.Framework;

namespace BattleRoyale.Tests.EditMode
{
    public sealed class SpiderChartLayoutTests
    {
        [Test]
        public void ChartHasSixAxesAndFiveRings()
        {
            Assert.AreEqual(AgentRollRules.StatCount, SpiderChartLayout.AxisCount);
            Assert.AreEqual(5, SpiderChartLayout.RingCount);
            Assert.IsTrue(SpiderChartLayout.TryRingRadius(0, out var inner));
            Assert.AreEqual(0.2f, inner, 0.0001f);
            Assert.IsTrue(SpiderChartLayout.TryRingRadius(4, out var outer));
            Assert.AreEqual(1f, outer, 0.0001f);
            Assert.IsFalse(SpiderChartLayout.TryRingRadius(5, out _));
        }

        [Test]
        public void AccuracyAtOneHundredPointsUp()
        {
            Assert.IsTrue(SpiderChartLayout.TryUnitPoint(0, 100, out var x, out var y));
            Assert.AreEqual(0f, x, 0.0001f);
            Assert.AreEqual(1f, y, 0.0001f);
        }

        [Test]
        public void AxesStepClockwiseBySixtyDegrees()
        {
            Assert.IsTrue(SpiderChartLayout.TryUnitPoint(0, 100, out var x0, out var y0));
            Assert.IsTrue(SpiderChartLayout.TryUnitPoint(1, 100, out var x1, out var y1));
            Assert.AreEqual(0.5f, x0 * x1 + y0 * y1, 0.0001f);
            Assert.Less(x0 * y1 - y0 * x1, 0f);

            Assert.IsTrue(SpiderChartLayout.TryUnitPoint(3, 100, out var x3, out var y3));
            Assert.AreEqual(0f, x3, 0.0001f);
            Assert.AreEqual(-1f, y3, 0.0001f);
        }

        [Test]
        public void SixtySitsOnTheSixtyRingAndOneHundredOnTheOuterRing()
        {
            Assert.IsTrue(SpiderChartLayout.TryUnitPoint(0, 60, out var x, out var y));
            Assert.IsTrue(SpiderChartLayout.TryRingPoint(2, 0, out var ringX, out var ringY));
            Assert.AreEqual(0f, x, 0.0001f);
            Assert.AreEqual(0.6f, y, 0.0001f);
            Assert.AreEqual(ringX, x, 0.0001f);
            Assert.AreEqual(ringY, y, 0.0001f);

            Assert.IsTrue(SpiderChartLayout.TryUnitPoint(2, 100, out var fullX, out var fullY));
            Assert.IsTrue(SpiderChartLayout.TryRingPoint(4, 2, out var outerX, out var outerY));
            Assert.AreEqual(outerX, fullX, 0.0001f);
            Assert.AreEqual(outerY, fullY, 0.0001f);
        }

        [Test]
        public void ALowMiddleValuePullsTheEdgeIn()
        {
            var values = new[] { 50, 10, 60, 0, 0, 0 };
            Assert.IsTrue(SpiderChartLayout.TryUnitPoint(0, 50, out var highLeftX, out var highLeftY));
            Assert.IsTrue(SpiderChartLayout.TryUnitPoint(2, 60, out var highRightX, out var highRightY));
            Assert.IsFalse(Covered(
                (highLeftX + highRightX) * 0.5f,
                (highLeftY + highRightY) * 0.5f,
                values));

            Assert.IsTrue(SpiderChartLayout.TryUnitPoint(1, 5, out var insideX, out var insideY));
            Assert.IsTrue(Covered(insideX, insideY, values));
            Assert.IsTrue(SpiderChartLayout.TryUnitPoint(1, 11, out var pastX, out var pastY));
            Assert.IsFalse(Covered(pastX, pastY, values));
        }

        [Test]
        public void ZeroIsTheCenter()
        {
            Assert.IsTrue(SpiderChartLayout.TryUnitPoint(4, 0, out var x, out var y));
            Assert.AreEqual(0f, x, 0.0001f);
            Assert.AreEqual(0f, y, 0.0001f);
        }

        [Test]
        public void RejectsValuesOutsideTheRings()
        {
            Assert.IsFalse(SpiderChartLayout.HasValues(null));
            Assert.IsFalse(SpiderChartLayout.HasValues(new[] { 10, 20, 30, 40, 50 }));
            Assert.IsFalse(SpiderChartLayout.HasValues(new[] { 10, 20, 30, 40, 50, 101 }));
            Assert.IsFalse(SpiderChartLayout.TryUnitPoint(0, -1, out _, out _));
            Assert.IsTrue(SpiderChartLayout.HasValues(new[] { 0, 20, 40, 60, 80, 100 }));
        }

        static bool Covered(float x, float y, int[] values)
        {
            for (var sector = 0; sector < SpiderChartLayout.AxisCount; sector++)
            {
                if (!SpiderChartLayout.TryFillTriangle(sector, out _, out var from, out var to))
                {
                    continue;
                }

                var fromAxis = from - 1;
                var toAxis = to - 1;
                if (!SpiderChartLayout.TryUnitPoint(fromAxis, values[fromAxis], out var ax, out var ay) ||
                    !SpiderChartLayout.TryUnitPoint(toAxis, values[toAxis], out var bx, out var by))
                {
                    continue;
                }

                if (InsideTriangle(x, y, 0f, 0f, ax, ay, bx, by))
                {
                    return true;
                }
            }

            return false;
        }

        static bool InsideTriangle(
            float px,
            float py,
            float ax,
            float ay,
            float bx,
            float by,
            float cx,
            float cy)
        {
            var v0x = cx - ax;
            var v0y = cy - ay;
            var v1x = bx - ax;
            var v1y = by - ay;
            var v2x = px - ax;
            var v2y = py - ay;
            var dot00 = v0x * v0x + v0y * v0y;
            var dot01 = v0x * v1x + v0y * v1y;
            var dot02 = v0x * v2x + v0y * v2y;
            var dot11 = v1x * v1x + v1y * v1y;
            var dot12 = v1x * v2x + v1y * v2y;
            var denom = dot00 * dot11 - dot01 * dot01;
            if (System.Math.Abs(denom) < 0.0000001f)
            {
                return false;
            }

            var u = (dot11 * dot02 - dot01 * dot12) / denom;
            var v = (dot00 * dot12 - dot01 * dot02) / denom;
            return u >= -0.0001f && v >= -0.0001f && u + v <= 1.0001f;
        }
    }
}
