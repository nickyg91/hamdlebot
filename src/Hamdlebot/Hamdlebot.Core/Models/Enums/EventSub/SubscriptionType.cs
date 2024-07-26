namespace Hamdlebot.Core.Models.Enums.EventSub;

public enum SubscriptionType
{
    AutomodMessageHold = 1, 
    AutomodMessageUpdate, 
    AutomodSettingsUpdate, 
    AutomodTermsUpdate, 
    ChannelUpdate,
    ChannelFollow,
    ChannelAdBreakBegin,
    ChannelChatClear,
    ChannelChatClearUserMessages,
    ChannelChatMessage,
    ChannelChatMessageDelete,
    ChannelChatNotification,
    ChannelChatSettingsUpdate,
    ChannelChatUserMessageHold, 
    ChannelChatUserMessageUpdate, 
    ChannelSubscribe,
    ChannelSubscriptionEnd,
    ChannelSubscriptionGift,
    ChannelSubscriptionMessage,
    ChannelCheer,
    ChannelRaid,
    ChannelRaidSent, //custom
    ChannelRaidReceived, //custom
    ChannelBan,
    ChannelUnban, 
    ChannelUnbanRequestCreate, 
    ChannelUnbanRequestResolve,
    ChannelModerate,
    ChannelModeratorAdd, 
    ChannelModeratorRemove, 
    ChannelGuestStarSessionBegin, // beta
    ChannelGuestStarSessionEnd, // beta
    ChannelGuestStarGuestUpdate, // beta
    ChannelGuestStarSettingsUpdate, // beta
    ChannelPointsAutomaticRewardRedemption,
    ChannelPointsCustomRewardAdd,
    ChannelPointsCustomRewardUpdate,
    ChannelPointsCustomRewardRemove,
    ChannelPointsCustomRewardRedemptionAdd,
    ChannelPointsCustomRewardRedemptionUpdate,
    ChannelPollBegin,
    ChannelPollProgress,
    ChannelPollEnd,
    ChannelPredictionBegin,
    ChannelPredictionProgress,
    ChannelPredictionLock,
    ChannelPredictionEnd,
    ChannelSuspiciousUserMessage,
    ChannelSuspiciousUserUpdate,
    ChannelVipAdd,
    ChannelVipRemove,
    ChannelWarningAcknowledgement,
    ChannelWarningSend,
    CharityDonation,
    CharityCampaignStart,
    CharityCampaignProgress,
    CharityCampaignStop,
    //ConduitShardDisabled,
    //DropEntitlementGrant, // Webhooks Only
    //ExtensionBitsTransactionCreate, // Webhooks Only
    GoalBegin,
    GoalProgress,
    GoalEnd,
    HypeTrainBegin,
    HypeTrainProgress,
    HypeTrainEnd,
    ShieldModeBegin,
    ShieldModeEnd,
    ShoutoutCreate,
    ShoutoutReceive,
    StreamOnline,
    StreamOffline,
    //UserAuthorizationGrant, // Webhooks Only
    //UserAuthorizationRevoke, // Webhooks Only
    UserUpdate, 
    WhisperReceived,
    NotSupported
}