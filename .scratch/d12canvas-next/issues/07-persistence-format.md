# Persistence/serialization format

Type: grilling
Status: open
Blocked by: 06

## Question

Design the local persistence/serialization format for a board (schema shape, versioning strategy for future migrations) built on the state/data model from ticket 06. It must stay import/export-ready — the format shouldn't need to change shape to later support exporting to or importing from other formats — even though building actual import/export is out of scope for now.
