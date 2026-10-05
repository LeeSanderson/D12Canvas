using D12Canvas.Model;

namespace D12Canvas.Tests;

// One saved board that needs every kind of group repair at once: a group with a member id that
// resolves to nothing, a group whose members are all gone, and a hand-written one-member group
// nested inside a parent. Both load paths must turn it into the same board.
internal static class GroupRepairFixtures
{
    public const string LeafA = "11111111-1111-1111-1111-111111111111";
    public const string LeafB = "22222222-2222-2222-2222-222222222222";
    public const string LeafC = "33333333-3333-3333-3333-333333333333";
    public const string LeafD = "44444444-4444-4444-4444-444444444444";

    public const string GroupWithDeadMember = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    public const string EmptiedGroup = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
    public const string OneMemberGroup = "cccccccc-cccc-cccc-cccc-cccccccccccc";
    public const string ParentGroup = "dddddddd-dddd-dddd-dddd-dddddddddddd";

    public const string DeadMember = "99999999-9999-9999-9999-999999999999";
    public const string DeadMemberTwo = "88888888-8888-8888-8888-888888888888";
    public const string DeadMemberThree = "77777777-7777-7777-7777-777777777777";

    public static readonly string Json = $$"""
        {
          "SchemaVersion": 1,
          "Components": [
            {{LeafComponent(LeafA)}},
            {{LeafComponent(LeafB)}},
            {{LeafComponent(LeafC)}},
            {{LeafComponent(LeafD)}}
          ],
          "Groups": [
            { "Id": "{{GroupWithDeadMember}}", "MemberIds": [ "{{LeafA}}", "{{LeafB}}", "{{DeadMember}}" ] },
            { "Id": "{{EmptiedGroup}}", "MemberIds": [ "{{DeadMemberTwo}}", "{{DeadMemberThree}}" ] },
            { "Id": "{{OneMemberGroup}}", "MemberIds": [ "{{LeafC}}" ] },
            { "Id": "{{ParentGroup}}", "MemberIds": [ "{{OneMemberGroup}}", "{{LeafD}}" ] }
          ]
        }
        """;

    public static readonly IReadOnlyList<string> RepairedGroups =
    [
        $"{GroupWithDeadMember}: {LeafA}, {LeafB}",
        $"{ParentGroup}: {LeafC}, {LeafD}",
    ];

    public static IReadOnlyList<string> Describe(Board board) =>
        board
            .Groups.Select(group =>
                $"{group.Id}: {string.Join(", ", group.MemberIds.Select(id => id.ToString()))}"
            )
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToList();

    public static string LeafComponent(string id) =>
        $$"""
            { "Id": "{{id}}", "ComponentTypeKey": "test-props", "Props": { "Text": "leaf" }, "Bounds": { "X": 0, "Y": 0, "Width": 10, "Height": 10 }, "ZIndex": 0 }
            """;
}
