using System.Text.Json;
using System.Text.Json.Serialization;
using Hamdlebot.Core.Models.Enums;
using Hamdlebot.Core.Models.Enums.EventSub;

namespace Hamdlebot.Core.Converters;

public class SubscriptionTypeEnumConverter : JsonConverter<SubscriptionType>
{
	public override SubscriptionType Read
		(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = reader.GetString();
		if (string.IsNullOrEmpty(value))
		{
			return SubscriptionType.NotSupported;
		}

		return value switch
		{
			"automod.message.hold" => SubscriptionType.AutomodMessageHold,
			"automod.message.update" => SubscriptionType.AutomodMessageUpdate,
			"automod.settings.update" => SubscriptionType.AutomodSettingsUpdate,
			"automod.terms.update" => SubscriptionType.AutomodTermsUpdate,
			"channel.update" => SubscriptionType.ChannelUpdate,
			"channel.follow" => SubscriptionType.ChannelFollow,
			"channel.ad_break.begin" => SubscriptionType.ChannelAdBreakBegin,
			"channel.chat.clear" => SubscriptionType.ChannelChatClear,
			"channel.chat.clear_user_messages" => SubscriptionType.ChannelChatClearUserMessages,
			"channel.chat.message" => SubscriptionType.ChannelChatMessage,
			"channel.chat.message_delete" => SubscriptionType.ChannelChatMessageDelete,
			"channel.chat.notification" => SubscriptionType.ChannelChatNotification,
			"channel.chat_settings.update" => SubscriptionType.ChannelChatSettingsUpdate,
			"channel.chat.user_message_hold" => SubscriptionType.ChannelChatUserMessageHold,
			"channel.chat.user_message_update" => SubscriptionType.ChannelChatUserMessageUpdate,
			"channel.subscribe" => SubscriptionType.ChannelSubscribe,
			"channel.subscription.end" => SubscriptionType.ChannelSubscriptionEnd,
			"channel.subscription.gift" => SubscriptionType.ChannelSubscriptionGift,
			"channel.subscription.message" => SubscriptionType.ChannelSubscriptionMessage,
			"channel.cheer" => SubscriptionType.ChannelCheer,
			"channel.raid" => SubscriptionType.ChannelRaid,
			"channel.ban" => SubscriptionType.ChannelBan,
			"channe.unban" => SubscriptionType.ChannelUnban,
			"channel.unban_request.create" => SubscriptionType.ChannelUnbanRequestCreate,
			"channel.unban_request.resolve" => SubscriptionType.ChannelUnbanRequestResolve,
			"channel.moderate" => SubscriptionType.ChannelModerate,
			"channel.moderator.add" => SubscriptionType.ChannelModeratorAdd,
			"channel.moderator.remove" => SubscriptionType.ChannelModeratorRemove,
			"channel.guest_star_session.begin" => SubscriptionType.ChannelGuestStarSessionBegin,
			"channel.guest_star_session.end" => SubscriptionType.ChannelGuestStarSessionEnd,
			"channel.guest_star_guest.update" => SubscriptionType.ChannelGuestStarSettingsUpdate,
			"channel.guest_star_settings.update" => SubscriptionType.ChannelGuestStarSettingsUpdate,
			"channel.channel_points_automatic_reward_redemption.add" => SubscriptionType.ChannelPointsAutomaticRewardRedemption,
			"channel.channel_points_custom_reward.add" => SubscriptionType.ChannelPointsCustomRewardAdd,
			"channel.channel_points_custom_reward.update" => SubscriptionType.ChannelPointsCustomRewardUpdate,
			"channel.channel_points_custom_reward.remove" => SubscriptionType.ChannelPointsCustomRewardRemove,
			"channel.channel_points_custom_reward_redemption.add" => SubscriptionType.ChannelPointsCustomRewardRedemptionAdd,
			"channel.channel_points_custom_reward_redemption.update" => SubscriptionType.ChannelPointsCustomRewardRedemptionUpdate,
			"channel.poll.begin" => SubscriptionType.ChannelPollBegin,
			"channel.poll.progress" => SubscriptionType.ChannelPollProgress,
			"channel.poll.end" => SubscriptionType.ChannelPollEnd,
			"channel.prediction.begin" => SubscriptionType.ChannelPredictionBegin,
			"channel.prediction.progress" => SubscriptionType.ChannelPredictionProgress,
			"channel.prediction.lock" => SubscriptionType.ChannelPredictionLock,
			"channel.prediction.end" => SubscriptionType.ChannelPredictionEnd,
			"channel.suspicious_user.message" => SubscriptionType.ChannelSuspiciousUserMessage,
			"channel.suspicious_user.update" => SubscriptionType.ChannelSuspiciousUserUpdate,
			"channel.vip.add" => SubscriptionType.ChannelVipAdd,
			"channel.vip.remove" => SubscriptionType.ChannelVipRemove,
			"channel.warning.acknowledge" => SubscriptionType.ChannelWarningAcknowledgement,
			"channel.warning.send" => SubscriptionType.ChannelWarningSend,
			"channel.charity_campaign.donate" => SubscriptionType.CharityDonation,
			"channel.charity_campaign.start" => SubscriptionType.CharityCampaignStart,
			"channel.charity_campaign.progress" => SubscriptionType.CharityCampaignProgress,
			"channel.charity_campaign.stop" => SubscriptionType.CharityCampaignStop,
			//"conduit.shard.disabled" => SubscriptionType.ConduitShardDisabled,
			//"drop.entitlement.grant" => SubscriptionType.DropEntitlementGrant,
			//"extension.bits_transaction.create" => SubscriptionType.ExtensionBitsTransactionCreate,
			"channel.goal.begin" => SubscriptionType.GoalBegin,
			"channel.goal.progress" => SubscriptionType.GoalProgress,
			"channel.goal.end" => SubscriptionType.GoalEnd,
			"channel.hype_train.begin" => SubscriptionType.HypeTrainBegin,
			"channel.hype_train.progress" => SubscriptionType.HypeTrainProgress,
			"channel.hype_train.end" => SubscriptionType.HypeTrainEnd,
			"channel.shield_mode.begin" => SubscriptionType.ShieldModeBegin,
			"channel.shield_mode.end" => SubscriptionType.ShieldModeEnd,
			"channel.shoutout.create" => SubscriptionType.ShoutoutCreate,
			"channel.shoutout.receive" => SubscriptionType.ShoutoutReceive,
			"stream.online" => SubscriptionType.StreamOnline,
			"stream.offline" => SubscriptionType.StreamOffline,
			//"user.authorization.grant" => SubscriptionType.UserAuthorizationGrant,
			//"user.authorization.revoke" => SubscriptionType.UserAuthorizationRevoke,
			"user.update" => SubscriptionType.UserUpdate,
			"user.whisper.message" => SubscriptionType.WhisperReceived,
			_ => SubscriptionType.NotSupported
		};
	}

	public override void Write
		(Utf8JsonWriter writer, SubscriptionType value, JsonSerializerOptions options)
	{
		var mappedValue = value switch
		{
			SubscriptionType.AutomodMessageHold => "automod.message.hold",
			SubscriptionType.AutomodMessageUpdate => "automod.message.update",
			SubscriptionType.AutomodSettingsUpdate => "automod.settings.update",
			SubscriptionType.AutomodTermsUpdate => "automod.terms.update",
			SubscriptionType.ChannelUpdate => "channel.update",
			SubscriptionType.ChannelFollow => "channel.follow",
			SubscriptionType.ChannelAdBreakBegin => "channel.ad_break.begin",
			SubscriptionType.ChannelChatClear => "channel.chat.clear",
			SubscriptionType.ChannelChatClearUserMessages => "channel.chat.clear_user_messages",
			SubscriptionType.ChannelChatMessage => "channel.chat.message",
			SubscriptionType.ChannelChatMessageDelete => "channel.chat.message_delete",
			SubscriptionType.ChannelChatNotification => "channel.chat.notification",
			SubscriptionType.ChannelChatSettingsUpdate => "channel.chat_settings.update",
			SubscriptionType.ChannelChatUserMessageHold => "channel.chat.user_message_hold",
			SubscriptionType.ChannelChatUserMessageUpdate => "channel.chat.user_message_update",
			SubscriptionType.ChannelSubscribe => "channel.subscribe",
			SubscriptionType.ChannelSubscriptionEnd => "channel.subscription.end",
			SubscriptionType.ChannelSubscriptionGift => "channel.subscription.gift",
			SubscriptionType.ChannelSubscriptionMessage => "channel.subscription.message",
			SubscriptionType.ChannelCheer => "channel.cheer",
			SubscriptionType.ChannelRaid => "channel.raid",
			SubscriptionType.ChannelRaidSent => "channel.raid", // custom
			SubscriptionType.ChannelRaidReceived => "channel.raid", // custom
			SubscriptionType.ChannelBan => "channel.ban",
			SubscriptionType.ChannelUnban => "channel.unban",
			SubscriptionType.ChannelUnbanRequestCreate => "channel.unban_request.create",
			SubscriptionType.ChannelUnbanRequestResolve => "channel.unban_request.resolve",
			SubscriptionType.ChannelModerate => "channel.moderate",
			SubscriptionType.ChannelModeratorAdd => "channel.moderator.add",
			SubscriptionType.ChannelModeratorRemove => "channel.moderator.remove",
			SubscriptionType.ChannelGuestStarSessionBegin => "channel.guest_star_session.begin",
			SubscriptionType.ChannelGuestStarSessionEnd => "channel.guest_star_session.end",
			SubscriptionType.ChannelGuestStarGuestUpdate => "channel.guest_star_guest.update",
			SubscriptionType.ChannelGuestStarSettingsUpdate => "channel.guest_star_settings.update",
			SubscriptionType.ChannelPointsAutomaticRewardRedemption => "channel.channel_points_automatic_reward_redemption.add",
			SubscriptionType.ChannelPointsCustomRewardAdd => "channel.channel_points_custom_reward.add",
			SubscriptionType.ChannelPointsCustomRewardUpdate => "channel.channel_points_custom_reward.update",
			SubscriptionType.ChannelPointsCustomRewardRemove => "channel.channel_points_custom_reward.remove",
			SubscriptionType.ChannelPointsCustomRewardRedemptionAdd => "channel.channel_points_custom_reward_redemption.add",
			SubscriptionType.ChannelPointsCustomRewardRedemptionUpdate => "channel.channel_points_custom_reward_redemption.update",
			SubscriptionType.ChannelPollBegin => "channel.poll.begin",
			SubscriptionType.ChannelPollProgress => "channel.poll.progress",
			SubscriptionType.ChannelPollEnd => "channel.poll.end",
			SubscriptionType.ChannelPredictionBegin => "channel.prediction.begin",
			SubscriptionType.ChannelPredictionProgress => "channel.prediction.progress",
			SubscriptionType.ChannelPredictionLock => "channel.prediction.lock",
			SubscriptionType.ChannelPredictionEnd => "channel.prediction.end",
			SubscriptionType.ChannelSuspiciousUserMessage => "channel.suspicious_user.message",
			SubscriptionType.ChannelSuspiciousUserUpdate => "channel.suspicious_user.update",
			SubscriptionType.ChannelVipAdd => "channel.vip.add",
			SubscriptionType.ChannelVipRemove => "channel.vip.remove",
			SubscriptionType.ChannelWarningAcknowledgement => "channel.warning.acknowledge",
			SubscriptionType.ChannelWarningSend => "channel.warning.send",
			SubscriptionType.CharityDonation => "channel.charity_campaign.donate",
			SubscriptionType.CharityCampaignStart => "channel.charity_campaign.start",
			SubscriptionType.CharityCampaignProgress => "channel.charity_campaign.progress",
			SubscriptionType.CharityCampaignStop => "channel.charity_campaign.stop",
			//SubscriptionType.ConduitShardDisabled => "conduit.shard.disabled",
			//SubscriptionType.DropEntitlementGrant => "drop.entitlement.grant",
			//SubscriptionType.ExtensionBitsTransactionCreate => "extension.bits_transaction.create",
			SubscriptionType.GoalBegin => "channel.goal.begin",
			SubscriptionType.GoalProgress => "channel.goal.progress",
			SubscriptionType.GoalEnd => "channel.goal.end",
			SubscriptionType.HypeTrainBegin => "channel.hype_train.begin",
			SubscriptionType.HypeTrainProgress => "channel.hype_train.progress",
			SubscriptionType.HypeTrainEnd => "channel.hype_train.end",
			SubscriptionType.ShieldModeBegin => "channel.shield_mode.begin",
			SubscriptionType.ShieldModeEnd => "channel.shield_mode.end",
			SubscriptionType.ShoutoutCreate => "channel.shoutout.create",
			SubscriptionType.ShoutoutReceive => "channel.shoutout.receive",
			SubscriptionType.StreamOnline => "stream.online",
			SubscriptionType.StreamOffline => "stream.offline",
			//SubscriptionType.UserAuthorizationGrant => "user.authorization.grant",
			//SubscriptionType.UserAuthorizationRevoke => "user.authorization.revoke",
			SubscriptionType.UserUpdate => "user.update",
			SubscriptionType.WhisperReceived => "user.whisper.message",
			SubscriptionType.NotSupported => "",

			// custom exception here at some point
			_ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
		};
		if (writer.CurrentDepth.Equals(1))
		{
			writer.WriteStringValue(mappedValue);
		}
	}
}