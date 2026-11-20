using D12Canvas.BuiltIns;
using D12Canvas.History;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.JSInterop;

namespace D12Canvas;

public partial class DiagramCanvas
{
    private bool CanChangePicture => SelectedImages().Count > 0;

    private IReadOnlyList<ComponentInstance> SelectedImages()
    {
        if (Board is null || _selectedEdgeIds.Count > 0 || _selectedInstanceIds.Count == 0)
        {
            return [];
        }

        var instances = _selectedInstanceIds.Select(Board.GetComponent).ToList();
        return instances.All(ImagePicture.IsImage)
            ? instances.OfType<ComponentInstance>().Where(image => !image.Locked).ToList()
            : [];
    }

    private async Task<string?> StoreImage(HeldImage image)
    {
        if (_jsModule is null)
        {
            return null;
        }

        var bytes = await image.TakeBytes(_jsModule);
        return bytes.Length == 0 || Board is null ? null : Board.AddAsset(bytes, image.MimeType);
    }

    private async Task<IReadOnlyList<(HeldImage Image, string Url)>> StoreImages(
        IEnumerable<HeldImage> images
    )
    {
        var stored = new List<(HeldImage, string)>();
        foreach (var image in images)
        {
            if (await StoreImage(image) is { } url)
            {
                stored.Add((image, url));
            }
        }

        return stored;
    }

    private Board ImageFragment(
        IReadOnlyList<(HeldImage Image, string Url)> pictures,
        (double X, double Y) firstCentre
    )
    {
        var fragment = new Board();
        if (
            Registry.All.FirstOrDefault(candidate => candidate.Key == ImagePicture.ComponentKey)
            is not { DefaultProps: ImageProps defaults } registration
        )
        {
            return fragment;
        }

        var fallback = registration.DefaultSize ?? new ComponentSize(FallbackWidth, FallbackHeight);
        var viewport = _zoomPanTracker.Viewport;
        for (var index = 0; index < pictures.Count; index++)
        {
            var (image, url) = pictures[index];
            var size = ImagePicture.SizeFor(image.Width, image.Height, viewport, fallback);
            var step = index * PasteCascade.Step;
            fragment.AddComponent(
                new ComponentInstance(
                    registration.Key,
                    defaults with
                    {
                        Url = url,
                    },
                    SnapBounds(
                        new Bounds(
                            firstCentre.X + step - size.Width / 2,
                            firstCentre.Y + step - size.Height / 2,
                            size.Width,
                            size.Height
                        )
                    ),
                    index
                )
            );
        }

        return fragment;
    }

    [JSInvokable]
    public async Task OnImageFilesDropped(
        HeldImage[] images,
        double x,
        double y,
        string[] hitEntityIds
    )
    {
        if (Board is null || PressOwnsBoard || PlacingPort)
        {
            return;
        }

        var dropPoint = ToBoardPoint((x, y), (0, 0));
        var hits = hitEntityIds
            .Select(id => Guid.TryParse(id, out var entityId) ? entityId : (Guid?)null)
            .OfType<Guid>()
            .ToList();
        var stored = await StoreImages(images);
        if (Board is null || PressOwnsBoard || stored.Count == 0)
        {
            return;
        }

        var commands = new List<ICommand>();
        var remaining = stored;
        if (ImagePicture.FillTarget(Board, hits) is { } target)
        {
            commands.Add(
                new MutateEntityCommand(
                    target,
                    target.Props,
                    ImagePicture.WithUrl(target.Props, stored[0].Url)
                )
            );
            remaining = stored.Skip(1).ToList();
        }

        BoardFragment.Placement? placement = null;
        var fragment = ImageFragment(remaining, dropPoint);
        if (fragment.Components.Count > 0)
        {
            placement = BoardFragment.PlaceOnto(Board, fragment);
            commands.Add(placement.Command);
        }

        if (commands.Count == 0)
        {
            return;
        }

        _history.Do(new CompositeCommand(commands));
        _contextMenu = null;
        if (placement is not null)
        {
            StepOutWhile(_ => true);
            SetSelection(placement.TopLevelIds, []);
        }

        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnImagesPasted(HeldImage[] images, double? pointerX, double? pointerY)
    {
        var anchor =
            pointerX is { } x && pointerY is { } y
                ? ToBoardPoint((x, y), (0, 0))
                : ViewportCentre();
        await PasteImages(images, anchor);
    }

    private async Task PasteImages(IEnumerable<HeldImage> images, (double X, double Y) anchor)
    {
        if (Board is null || PressOwnsBoard || PlacingPort)
        {
            return;
        }

        var stored = await StoreImages(images);
        if (stored.Count > 0)
        {
            PasteFragment(ImageFragment(stored, (0, 0)), anchor, []);
        }
    }

    private async Task ChoosePictureFromMenu()
    {
        if (_jsModule is null || PressOwnsBoard || SelectedImages().Count == 0)
        {
            return;
        }

        var picked = await _jsModule.InvokeAsync<HeldImage?>("chooseImageFile");
        if (picked is not null && await StoreImage(picked) is { } url)
        {
            SetPictureOfSelectedImages(url);
        }
    }

    private void SetPictureOfSelectedImages(string url)
    {
        if (PressOwnsBoard)
        {
            return;
        }

        CommitPropsChangeBatch(
            SelectedImages()
                .Where(image => ((ImageProps)image.Props).Url != url)
                .Select(image => new PropsChange(
                    image.Id,
                    image.Props,
                    ImagePicture.WithUrl(image.Props, url)
                ))
                .ToList()
        );
    }
}
