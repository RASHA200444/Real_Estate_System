namespace otherServices.Models.Enums
{
    public enum NotificationType
    {
        // 1. Onboarding & Identity (Ownership)
        OwnershipDocumentReceived = 1,
        OwnershipVerified = 2,
        OwnershipRejected = 3,

        // 2. Post Lifecycle
        PostApproved = 10,
        PostRejected = 11,
        PostDeactivated = 12,
        ProPostAlert = 13, // لاندلورد برو نزل عقار
        FavoritePostUpdated = 14, // عقار في المفضلة اتعدل
        NewPost = 15,

        // 3. Proposals & Contracts
        NewProposal = 20,
        ProposalAccepted = 21,
        ProposalRejected = 22,
        ContractSignedByOneParty = 23, // طرف واحد مضى
        ContractFullyVerified = 24,    // الطرفين مضوا

        // 4. Pro Subscription
        SubscriptionActivated = 30,
        SubscriptionExpiringSoon = 31,
        SubscriptionExpired = 32,

        // 5. Payments
        PaymentRequired = 40,
        InstallmentReminder = 41,
        PaymentSuccessful = 42,
        PaymentFailed = 43,

        // 6. Chat (From Python/Mongo via Kafka)
        NewMessage = 50
    }
}