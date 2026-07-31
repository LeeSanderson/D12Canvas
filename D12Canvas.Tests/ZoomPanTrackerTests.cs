using D12Canvas;
using D12Canvas.Model;
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
            _eventTriggered = false;
            _lastEventArgs = null;

            _tracker.Pan(-10, -10);

            Assert.True(_eventTriggered);
            Assert.NotNull(_lastEventArgs);

            Assert.Equal(1.0, _lastEventArgs!.Scale);
            Assert.Equal(-10, _lastEventArgs!.PanX);
            Assert.Equal(-10, _lastEventArgs!.PanY);
        }

        [Fact]
        public void ZoomInTriggersChangedEvent()
        {
            _eventTriggered = false;
            _lastEventArgs = null;

            _tracker.Zoom(true);

            Assert.True(_eventTriggered);
            Assert.NotNull(_lastEventArgs);

            Assert.Equal(1.1, _lastEventArgs!.Scale);
            Assert.Equal(0, _lastEventArgs!.PanX);
            Assert.Equal(0, _lastEventArgs!.PanY);
        }

        [Fact]
        public void ZoomOutTriggersChangedEvent()
        {
            _eventTriggered = false;
            _lastEventArgs = null;

            _tracker.Zoom(false);

            Assert.True(_eventTriggered);
            Assert.NotNull(_lastEventArgs);

            Assert.Equal(0.9, _lastEventArgs!.Scale);
            Assert.Equal(0, _lastEventArgs!.PanX);
            Assert.Equal(0, _lastEventArgs!.PanY);
        }

        [Fact]
        public void PanRespectsContainerBoundaries()
        {
            _tracker.SetContainerSize(100, 100);
            _tracker.SetCanvasSize(1000, 1000);

            _tracker.Pan(100, 0);
            Assert.Equal(0, _tracker.PanX);

            _tracker.Pan(-1000, 0);
            Assert.Equal(-900, _tracker.PanX); // -900 = 1000 - 100
        }

        [Fact]
        public void PanRespectsScale()
        {
            _tracker.SetContainerSize(100, 100);
            _tracker.SetCanvasSize(1000, 1000);
            _tracker.Zoom(true);

            _tracker.Pan(100, 0);
            Assert.Equal(0, _tracker.PanX);

            _tracker.Pan(-1000, 0);
            Assert.Equal(-1000, _tracker.PanX); // (1000 * 1.1) - 100 = 1000

            _tracker.Pan(0, 100);
            Assert.Equal(0, _tracker.PanY);

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
            _tracker.SetContainerSize(800, 600);

            _eventTriggered = false;
            _lastEventArgs = null;

            _tracker.SetContainerSize(1200, 800);

            Assert.True(_eventTriggered);
            Assert.NotNull(_lastEventArgs);

            Assert.Equal(1200, _lastEventArgs!.ContainerWidth);
            Assert.Equal(800, _lastEventArgs!.ContainerHeight);
        }

        [Fact]
        public void SetCanvasSizeTriggersChangedEvent()
        {
            _tracker.SetCanvasSize(1000, 1000);

            _eventTriggered = false;
            _lastEventArgs = null;

            // Container/scale stay put, so the pan position itself never needs clamping here -
            // this must still notify.
            _tracker.SetCanvasSize(2000, 2000);

            Assert.True(_eventTriggered);
            Assert.NotNull(_lastEventArgs);
        }

        [Fact]
        public void ViewportIsTheFullContainerAtDefaultPanAndScale()
        {
            _tracker.SetContainerSize(800, 600);

            Assert.Equal(new Bounds(0, 0, 800, 600), _tracker.Viewport);
        }

        [Fact]
        public void ViewportShiftsOppositeThePanOffset()
        {
            _tracker.SetContainerSize(800, 600);
            _tracker.SetCanvasSize(3000, 3000);
            _tracker.Pan(-50, -60);

            Assert.Equal(new Bounds(50, 60, 800, 600), _tracker.Viewport);
        }

        [Fact]
        public void ViewportShrinksAsScaleIncreases()
        {
            _tracker.SetContainerSize(800, 600);
            _tracker.Zoom(true); // scale -> 1.1

            Assert.Equal(new Bounds(0, 0, 800 / 1.1, 600 / 1.1), _tracker.Viewport);
        }
    }
}
