using D12Canvas;
using Xunit;

namespace D12Canvas.Tests
{
    public class ZoomPanTrackerTests
    {
        private readonly ZoomPanTracker _tracker;
        private bool _eventTriggered;
        private ZoomPanChangedEventArgs? _lastEventArgs;

        public ZoomPanTrackerTests()
        {
            _tracker = new ZoomPanTracker();
            _tracker.SetContainerSize(100, 100);
            _tracker.Changed += (sender, args) =>
            {
                _eventTriggered = true;
                _lastEventArgs = args;
            };
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
            _tracker.Scale = 6.0;
            Assert.False(_tracker.ZoomIn());
        }

        [Fact]
        public void ZoomOutRespectsMinScale()
        {
            _tracker.Scale = 0.6;
            Assert.False(_tracker.ZoomOut());
        }

        [Fact]
        public void PanTriggersChangedEvent()
        {
            // Reset event tracking
            _eventTriggered = false;
            _lastEventArgs = null;

            // Pan by 10 pixels in both directions
            _tracker.Pan(-10, -10);

            // Verify event was triggered
            Assert.True(_eventTriggered);
            Assert.NotNull(_lastEventArgs);

            // Verify event args contain correct values
            Assert.Equal(1.0, _lastEventArgs!.Scale);
            Assert.Equal(-10, _lastEventArgs!.PanX);
            Assert.Equal(-10, _lastEventArgs!.PanY);
        }

        [Fact]
        public void ZoomInTriggersChangedEvent()
        {
            // Reset event tracking
            _eventTriggered = false;
            _lastEventArgs = null;

            // Zoom in
            _tracker.Zoom(true);

            // Verify event was triggered
            Assert.True(_eventTriggered);
            Assert.NotNull(_lastEventArgs);

            // Verify event args contain correct values
            Assert.Equal(1.1, _lastEventArgs!.Scale);
            Assert.Equal(0, _lastEventArgs!.PanX);
            Assert.Equal(0, _lastEventArgs!.PanY);
        }

        [Fact]
        public void ZoomOutTriggersChangedEvent()
        {
            // Reset event tracking
            _eventTriggered = false;
            _lastEventArgs = null;

            // Zoom out
            _tracker.Zoom(false);

            // Verify event was triggered
            Assert.True(_eventTriggered);
            Assert.NotNull(_lastEventArgs);

            // Verify event args contain correct values
            Assert.Equal(0.9, _lastEventArgs!.Scale);
            Assert.Equal(0, _lastEventArgs!.PanX);
            Assert.Equal(0, _lastEventArgs!.PanY);
        }

        [Fact]
        public void PanRespectsContainerBoundaries()
        {
            // Set up a small container and large canvas
            _tracker.SetContainerSize(100, 100);
            _tracker.SetCanvasSize(1000, 1000);

            // Try to pan too far right
            _tracker.Pan(100, 0);
            Assert.Equal(0, _tracker.PanX); // Should be constrained to 0

            // Try to pan too far left
            _tracker.Pan(-1000, 0);
            Assert.Equal(-900, _tracker.PanX); // Should be constrained to -900 (1000 - 100)
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

        [Fact]
        public void SetContainerSizeUpdatesEventArgs()
        {
            // Set initial container size
            _tracker.SetContainerSize(800, 600);

            // Reset event tracking
            _eventTriggered = false;
            _lastEventArgs = null;

            // Update container size
            _tracker.SetContainerSize(1200, 800);

            // Verify event was triggered
            Assert.True(_eventTriggered);
            Assert.NotNull(_lastEventArgs);

            // Verify event args contain correct container dimensions
            Assert.Equal(1200, _lastEventArgs!.ContainerWidth);
            Assert.Equal(800, _lastEventArgs!.ContainerHeight);
        }
    }
}
