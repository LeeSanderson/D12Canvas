using D12Canvas;
using Xunit;

namespace D12Canvas.Tests
{
    public class ZoomPanTrackerTests
    {
        private readonly ZoomPanTracker _tracker;

        public ZoomPanTrackerTests()
        {
            _tracker = new ZoomPanTracker();
            _tracker.SetContainerSize(100, 100);
        }

        [Fact]
        public void InitialScaleIsOne()
        {
            Assert.Equal(1.0, _tracker.Scale);
        }

        [Fact]
        public void InitialPanPositionIsZero()
        {
            Assert.Equal(0.0, _tracker.PanX);
            Assert.Equal(0.0, _tracker.PanY);
        }

        [Fact]
        public void CanZoomIn()
        {
            var result = _tracker.Zoom(true);
            Assert.True(result);
            Assert.Equal(1.1, _tracker.Scale);
        }

        [Fact]
        public void CanZoomOut()
        {
            var result = _tracker.Zoom(false);
            Assert.True(result);
            Assert.Equal(0.9, _tracker.Scale);
        }

        [Fact]
        public void ZoomInRespectsMaxScale()
        {
            // Zoom in several times to get close to max scale
            for (int i = 0; i < 50; i++)
            {
                if (Math.Abs(_tracker.Scale - 6.0) < 0.2)
                {
                    var result = _tracker.Zoom(true);
                    Assert.True(result);
                    Assert.Equal(6.0, _tracker.Scale, 0.01);
                    return;
                }
                _tracker.Zoom(true);
            }

            Assert.Fail(
                string.Format(
                    "Could not reach max scale within 50 zooms, scale is {0}",
                    _tracker.Scale
                )
            );
        }

        [Fact]
        public void ZoomOutRespectsMinScale()
        {
            // Then zoom out several times to get close to min scale
            for (int i = 0; i < 50; i++)
            {
                if (Math.Abs(_tracker.Scale - 0.6) < 0.2)
                {
                    var result = _tracker.Zoom(false);
                    Assert.True(result);
                    Assert.Equal(0.6, _tracker.Scale, 0.01);
                    return;
                }
                _tracker.Zoom(false);
            }

            Assert.Fail(
                string.Format(
                    "Could not reach min scale within 50 zooms, scale is {0}",
                    _tracker.Scale
                )
            );
        }

        [Fact]
        public void PanRespectsContainerBoundaries()
        {
            // Set up a small container and large canvas
            _tracker.SetContainerSize(100, 100);
            _tracker.SetCanvasSize(1000, 1000);

            // Try to pan too far right
            _tracker.Pan(100, 0);
            Assert.Equal(0, _tracker.PanX);

            // Try to pan too far left
            _tracker.Pan(-1000, 0);
            Assert.Equal(-900, _tracker.PanX); // 1000 - 100 = 900

            // Try to pan too far down
            _tracker.Pan(0, 100);
            Assert.Equal(0, _tracker.PanY);

            // Try to pan too far up
            _tracker.Pan(0, -1000);
            Assert.Equal(-900, _tracker.PanY);
        }

        [Fact]
        public void PanRespectsScale()
        {
            _tracker.SetContainerSize(100, 100);
            _tracker.SetCanvasSize(1000, 1000);
            _tracker.Zoom(true); // Zoom in to 1.1

            // Try to pan too far right
            _tracker.Pan(100, 0);
            Assert.Equal(0, _tracker.PanX);

            // Try to pan too far left
            _tracker.Pan(-1000, 0);
            Assert.Equal(-1000, _tracker.PanX); // (1000 * 1.1) - 100 = 1000

            // Try to pan too far down
            _tracker.Pan(0, 100);
            Assert.Equal(0, _tracker.PanY);

            // Try to pan too far up
            _tracker.Pan(0, -1000);
            Assert.Equal(-1000, _tracker.PanY); // (1000 * 1.1) - 100 = 1000
        }

        [Fact]
        public void SetContainerSizePreventsNegativeValues()
        {
            Assert.Throws<ArgumentException>(() => _tracker.SetContainerSize(-100, 100));
            Assert.Throws<ArgumentException>(() => _tracker.SetContainerSize(100, -100));
        }

        [Fact]
        public void SetCanvasSizePreventsNegativeValues()
        {
            Assert.Throws<ArgumentException>(() => _tracker.SetCanvasSize(-100, 100));
            Assert.Throws<ArgumentException>(() => _tracker.SetCanvasSize(100, -100));
        }
    }
}
