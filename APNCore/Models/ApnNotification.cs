namespace APNCore.Models;

/// <summary>
/// A single notification to deliver to one device: the target token, the request headers and the
/// payload.
/// </summary>
public sealed class ApnNotification
{
    /// <summary>
    /// Creates a notification for the given device token and push type. Prefer the static factory
    /// methods (<see cref="Alert(string, string, string)"/>, <see cref="Background"/>, etc.) for common cases.
    /// </summary>
    /// <param name="deviceToken">The hexadecimal device token obtained from the app on the device.</param>
    /// <param name="pushType">The value for the required <c>apns-push-type</c> header.</param>
    public ApnNotification(string deviceToken, ApnPushType pushType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceToken);
        DeviceToken = deviceToken;
        PushType = pushType;
    }

    /// <summary>The hexadecimal device token that identifies the target device/app installation.</summary>
    public string DeviceToken { get; set; }

    /// <summary>The value for the required <c>apns-push-type</c> header.</summary>
    public ApnPushType PushType { get; set; }

    /// <summary>
    /// The topic for the <c>apns-topic</c> header. When <see langword="null"/>, the client's
    /// configured default topic is used. For most pushes this is the app's bundle ID; VoIP, Live
    /// Activity, complication, etc. require a suffixed topic (for example
    /// <c>com.example.app.voip</c>).
    /// </summary>
    public string? Topic { get; set; }

    /// <summary>The <c>apns-priority</c> header. When <see langword="null"/>, APNs uses its default (10).</summary>
    public ApnPriority? Priority { get; set; }

    /// <summary>
    /// The <c>apns-expiration</c> header: the point in time after which APNs stops attempting
    /// delivery. When <see langword="null"/>, no expiration header is sent (APNs stores and retries
    /// the notification). Use <see cref="DateTimeOffset.UnixEpoch"/> to request a single immediate
    /// delivery attempt with no storage.
    /// </summary>
    public DateTimeOffset? Expiration { get; set; }

    /// <summary>
    /// The <c>apns-collapse-id</c> header. Notifications that share a collapse identifier are
    /// coalesced so that only the most recent one is shown (maximum 64 bytes).
    /// </summary>
    public string? CollapseId { get; set; }

    /// <summary>
    /// The <c>apns-id</c> header: a canonical UUID that identifies the notification. When
    /// <see langword="null"/>, APNs generates one and returns it on the response.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>The notification payload (the JSON body).</summary>
    public ApnPayload Payload { get; set; } = new();

    // -- Factory helpers, one per common push type --------------------------------------------

    /// <summary>Creates a user-facing alert notification with a title and body.</summary>
    public static ApnNotification Alert(string deviceToken, string title, string body) =>
        new(deviceToken, ApnPushType.Alert)
        {
            Priority = ApnPriority.High,
            Payload = new ApnPayload
            {
                Aps = new Aps { Alert = new ApnAlert { Title = title, Body = body } },
            },
        };

    /// <summary>Creates a user-facing alert notification from a prebuilt <see cref="Aps"/> dictionary.</summary>
    public static ApnNotification Alert(string deviceToken, Aps aps) =>
        new(deviceToken, ApnPushType.Alert)
        {
            Priority = ApnPriority.High,
            Payload = new ApnPayload { Aps = aps },
        };

    /// <summary>
    /// Creates a silent background notification (<c>content-available: 1</c>) that wakes the app to
    /// fetch new content. Uses medium priority, as APNs requires for background pushes.
    /// </summary>
    public static ApnNotification Background(string deviceToken, IDictionary<string, object?>? customData = null) =>
        new(deviceToken, ApnPushType.Background)
        {
            Priority = ApnPriority.Medium,
            Payload = new ApnPayload
            {
                Aps = new Aps { ContentAvailable = 1 },
                CustomData = customData,
            },
        };

    /// <summary>Creates a VoIP (PushKit) notification. The topic must be your VoIP topic (bundle ID + <c>.voip</c>).</summary>
    public static ApnNotification Voip(string deviceToken, string voipTopic, IDictionary<string, object?>? customData = null) =>
        new(deviceToken, ApnPushType.Voip)
        {
            Topic = voipTopic,
            Priority = ApnPriority.High,
            Payload = new ApnPayload { CustomData = customData },
        };

    /// <summary>Creates a location-request notification. The topic must be your location topic (bundle ID + <c>.location-query</c>).</summary>
    public static ApnNotification Location(string deviceToken, string locationTopic) =>
        new(deviceToken, ApnPushType.Location)
        {
            Topic = locationTopic,
            Priority = ApnPriority.High,
            Payload = new ApnPayload(),
        };

    /// <summary>Creates a watchOS complication update. The topic must be your complication topic (bundle ID + <c>.complication</c>).</summary>
    public static ApnNotification Complication(string deviceToken, string complicationTopic, Aps? aps = null) =>
        new(deviceToken, ApnPushType.Complication)
        {
            Topic = complicationTopic,
            Payload = new ApnPayload { Aps = aps ?? new Aps() },
        };

    /// <summary>Creates a File Provider reload signal. The topic must be your File Provider topic (bundle ID + <c>.pushkit.fileprovider</c>).</summary>
    public static ApnNotification FileProvider(string deviceToken, string fileProviderTopic) =>
        new(deviceToken, ApnPushType.FileProvider)
        {
            Topic = fileProviderTopic,
            Payload = new ApnPayload { Aps = new Aps { ContentAvailable = 1 } },
        };

    /// <summary>Creates an MDM notification. The topic must be the push magic topic from the MDM enrollment.</summary>
    public static ApnNotification Mdm(string deviceToken, string mdmTopic, string pushMagic) =>
        new(deviceToken, ApnPushType.Mdm)
        {
            Topic = mdmTopic,
            Payload = new ApnPayload { CustomData = new Dictionary<string, object?>(StringComparer.Ordinal) { ["mdm"] = pushMagic } },
        };

    /// <summary>
    /// Creates a Live Activity (ActivityKit) notification. The topic must be your push-to-start or
    /// activity topic (bundle ID + <c>.push-type.liveactivity</c>). Use the <see cref="Aps.Event"/>,
    /// <see cref="Aps.ContentState"/> and related members to describe the start/update/end.
    /// </summary>
    public static ApnNotification LiveActivity(string deviceToken, string liveActivityTopic, Aps aps) =>
        new(deviceToken, ApnPushType.LiveActivity)
        {
            Topic = liveActivityTopic,
            Priority = ApnPriority.High,
            Payload = new ApnPayload { Aps = aps },
        };

    /// <summary>Creates a Push to Talk channel notification. The topic must be your PTT topic (bundle ID + <c>.voip-ptt</c>).</summary>
    public static ApnNotification PushToTalk(string deviceToken, string pttTopic, IDictionary<string, object?>? customData = null) =>
        new(deviceToken, ApnPushType.PushToTalk)
        {
            Topic = pttTopic,
            Priority = ApnPriority.High,
            Payload = new ApnPayload { CustomData = customData },
        };
}
