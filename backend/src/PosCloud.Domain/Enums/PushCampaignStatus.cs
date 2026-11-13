namespace PosCloud.Domain.Enums;

/// <summary>Push kampaniya holati.</summary>
public enum PushCampaignStatus
{
    Draft = 0,
    Scheduled = 1,
    Sending = 2,
    Sent = 3,
    Failed = 4
}
