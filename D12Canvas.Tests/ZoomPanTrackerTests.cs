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

        // No built-in ceiling/floor by default - the prototype's fixed 0.6x-6.0x range is gone,
        // so zooming well past either of its former edges must keep working.
        [Fact]
        public void ZoomInHasNoBuiltInCeilingByDefault()
        {
            _tracker.Scale = 6.0;
            Assert.True(_tracker.ZoomIn());
            Assert.Equal(6.1, _tracker.Scale);
        }

        [Fact]
        public void ZoomOutHasNoBuiltInFloorByDefault()
        {
            _tracker.Scale = 0.6;
            Assert.True(_tracker.ZoomOut());
            Assert.Equal(0.5, _tracker.Scale);
        }

        // A numerical-stability guard, not a product-facing limit: with no host-configured
        // MinZoom, repeated zoom-out must approach zero without ever reaching zero or going
        // negative (which would flip the canvas and break Viewport's division by Scale).
        [Fact]
        public void ZoomOutNeverReachesZeroOrNegativeWithNoMinZoomSet()
        {
            for (var i = 0; i < 1000; i++)
            {
                _tracker.ZoomOut();
            }

            Assert.True(_tracker.Scale > 0);
            Assert.True(double.IsFinite(_tracker.Scale));
        }

        [Fact]
        public void MaxZoomClampsZoomIn()
        {
            _tracker.SetZoomLimits(null, 2.0);
            _tracker.Scale = 2.0;

            Assert.False(_tracker.ZoomIn());
            Assert.Equal(2.0, _tracker.Scale);
        }

        [Fact]
        public void MinZoomClampsZoomOut()
        {
            _tracker.SetZoomLimits(0.5, null);
            _tracker.Scale = 0.5;

            Assert.False(_tracker.ZoomOut());
            Assert.Equal(0.5, _tracker.Scale);
        }

        // Setting a limit re-clamps whatever Scale already is, rather than waiting for the next
        // ZoomIn/ZoomOut - Scale must never sit outside the currently configured bounds.
        [Fact]
        public void SettingMaxZoomBelowTheCurrentScaleClampsItImmediately()
        {
            _tracker.Scale = 10.0;

            _tracker.SetZoomLimits(null, 3.0);

            Assert.Equal(3.0, _tracker.Scale);
        }

        [Fact]
        public void SettingMinZoomAboveTheCurrentScaleClampsItImmediately()
        {
            _tracker.Scale = 0.2;

            _tracker.SetZoomLimits(1.0, null);

            Assert.Equal(1.0, _tracker.Scale);
        }

        [Fact]
        public void MinZoomMustBePositive()
        {
            Assert.Throws<ArgumentException>(() => _tracker.SetZoomLimits(0.0, null));
            Assert.Throws<ArgumentException>(() => _tracker.SetZoomLimits(-1.0, null));
            Assert.Throws<ArgumentException>(() => _tracker.SetZoomLimits(double.NaN, null));
        }

        [Fact]
        public void MaxZoomMustBePositive()
        {
            Assert.Throws<ArgumentException>(() => _tracker.SetZoomLimits(null, 0.0));
            Assert.Throws<ArgumentException>(() => _tracker.SetZoomLimits(null, -1.0));
            Assert.Throws<ArgumentException>(() => _tracker.SetZoomLimits(null, double.NaN));
        }

        // Setting both at once so a host changing both together can never trip a transient
        // min > max state depending on which field this API happened to assign first.
        [Fact]
        public void MinZoomMustNotExceedMaxZoom()
        {
            Assert.Throws<ArgumentException>(() => _tracker.SetZoomLimits(5.0, 2.0));
        }

        [Fact]
        public void MinZoomEqualToMaxZoomIsAllowed()
        {
            _tracker.SetZoomLimits(2.0, 2.0);

            Assert.Equal(2.0, _tracker.MinZoom);
            Assert.Equal(2.0, _tracker.MaxZoom);
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

        // No fixed board extent - panning is never clamped, so content can be placed and panned
        // to arbitrarily far coordinates in any direction.
        [Fact]
        public void PanIsUnboundedInEveryDirection()
        {
            _tracker.SetContainerSize(100, 100);

            _tracker.Pan(1_000_000, 0);
            Assert.Equal(1_000_000, _tracker.PanX);

            _tracker.Pan(-2_000_000, 0);
            Assert.Equal(-1_000_000, _tracker.PanX);
        }

        [Fact]
        public void PanIsUnboundedRegardlessOfScale()
        {
            _tracker.SetContainerSize(100, 100);
            _tracker.Scale = 50.0;

            _tracker.Pan(-1_000_000, -1_000_000);

            Assert.Equal(-1_000_000, _tracker.PanX);
            Assert.Equal(-1_000_000, _tracker.PanY);
        }

        [Fact]
        public void SetContainerSizePreventsNegativeValues()
        {
            Assert.Throws<ArgumentException>(() => _tracker.SetContainerSize(-100, 100));
            Assert.Throws<ArgumentException>(() => _tracker.SetContainerSize(100, -100));
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
        public void ViewportIsTheFullContainerAtDefaultPanAndScale()
        {
            _tracker.SetContainerSize(800, 600);

            Assert.Equal(new Bounds(0, 0, 800, 600), _tracker.Viewport);
        }

        [Fact]
        public void ViewportShiftsOppositeThePanOffset()
        {
            _tracker.SetContainerSize(800, 600);
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

        [Fact]
        public void ViewportStaysFiniteAtExtremeButRealisticScaleAndPan()
        {
            _tracker.SetContainerSize(800, 600);
            _tracker.Scale = 500.0;
            _tracker.Pan(-1_000_000, -1_000_000);

            var viewport = _tracker.Viewport;

            Assert.True(double.IsFinite(viewport.X));
            Assert.True(double.IsFinite(viewport.Y));
            Assert.True(double.IsFinite(viewport.Width));
            Assert.True(double.IsFinite(viewport.Height));
        }
    }
}
