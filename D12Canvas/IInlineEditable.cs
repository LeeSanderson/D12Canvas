namespace D12Canvas;

// Implemented by a registered component type whose content is edited in place on the canvas. The
// canvas calls BeginEdit when the user asks to edit a mounted instance; a type that does not
// implement it has declined, and every route that would start an edit does nothing.
public interface IInlineEditable
{
    void BeginEdit();
}
