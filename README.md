Automatically scans the items upon landing and sends output in chat for everyone

## Configuration

1) AutoAnnounce (default: true)
    - Purpose: Automatically announces the number of items located outside the ship at the start of the round.
    - Behavior: When enabled, the mod performs a single automatic announcement at round start summarizing how many loot items are outside the ship.
    - When to disable: Turn off if you prefer manually scanning for items.

2) AnnounceOutsideLoot (default: true)
    - Purpose: Includes the total count of loot that is located outside the facility in announcements. This count includes Beehives and Sapsucker eggs.
    - Behavior: When enabled, any announcement about outside items will explicitly include discovered outside loot counts, and will account for Beehives and Sapsucker eggs in that total.
    - When to disable: Disable if you do not want these external loot counts revealed.

3) AnnounceValue (default: true)
    - Purpose: Announces the total loot value (approximate, identical to terminal scan command) when performing a scan/announcement.
    - Behavior: When enabled, announcements will include the aggregated value of the scanned loot in addition to counts.
    - When to disable: Disable if you want announcements to show only counts (not values), or to reduce information revealed to other players.

4) HostOnly (default: true)
    - Purpose: Restricts automated announcements to the host only.
    - Behavior: When enabled, the mod will not announce scan on lobbies you do not host.
    - When to disable: Disable to do the announcement as a client.