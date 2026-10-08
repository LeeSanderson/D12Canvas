namespace D12Canvas;

// Implemented by a registered component type whose content is edited in place on the canvas. The
// canvas calls BeginEdit after the render that mounts the instance; a type that does not implement
// it has declined, and every route that would start an edit does nothing. BeginEdit should focus
// the editor without scrolling and select all of its text. When the edit ends, on every route,
// the component calls DiagramCanvas.CommitInlineEdit once, naming itself by the cascaded
// "InstanceId" value on the cascaded "ParentCanvas", with returnFocus true only for Escape.
public interface IInlineEditable
{
    void BeginEdit();
}
