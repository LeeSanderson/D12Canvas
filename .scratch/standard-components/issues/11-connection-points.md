# Connection points

Type: prototype
Status: open
Blocked by:
Map: [Better standard components](../map.md)

## Question

What makes ports "better", and how do they look and behave?

Decide, with a throwaway Demo prototype to react to:

- when ports show: always, on hover of the shape, only while drawing or dragging an endpoint
- their size and hit target, and how they scale with zoom
- whether an endpoint may attach anywhere on a shape's outline (nearest point) as well as to fixed ports
- whether an endpoint may attach to the shape as a whole, with the router picking the side ("floating" attach)
- the highlight that shows which port or shape will receive the drop

## Comments

- From [Shape kinds](07-shape-kinds.md): a new registration hook maps props to the four standard port fractions, so every shape kind's standard ports are on its outline. Custom ports are still fractions of the bounding box, so on a diamond or ellipse one added in an empty corner floats off the outline. Decide here whether custom ports, and any attach-anywhere point, snap to the outline. That needs an outline hook the shape supplies per kind.
