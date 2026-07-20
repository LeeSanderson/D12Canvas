# Styling/theming and layering

Type: grilling
Status: open
Blocked by: 05, 06

## Question

Design styling/theming for component instances (fill/stroke/colors/fonts) and the layering UX built on the state model's explicit `ZIndex` field (e.g. bring-to-front/send-to-back, how z-order interacts with groups). Depends on the component registration contract (05) to know what a component type exposes as themeable, and the state/data model (06) for the `ZIndex` field it acts on.
