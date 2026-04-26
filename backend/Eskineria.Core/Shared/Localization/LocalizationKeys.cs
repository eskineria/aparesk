namespace Eskineria.Core.Shared.Localization;

public static class LocalizationKeys
{
    // Access Control Messages
    public const string RoleNameRequired = "RoleNameRequired";
    public const string RoleAlreadyExists = "RoleAlreadyExists";
    public const string RoleSwitchedSuccessfully = "RoleSwitchedSuccessfully";
    public const string RoleSwitchFailed = "RoleSwitchFailed";
    public const string UserDoesNotHaveSpecifiedRole = "UserDoesNotHaveSpecifiedRole";
    public const string CannotDeleteAdminRole = "CannotDeleteAdminRole";
    public const string RoleNotFound = "RoleNotFound";
    public const string UserNotFound = "UserNotFound";
    public const string FailedToRemoveCurrentRoles = "FailedToRemoveCurrentRoles";
    public const string FailedToAddNewRoles = "FailedToAddNewRoles";
    public const string FailedToUpdateUserStatus = "FailedToUpdateUserStatus";
    public const string UserStatusUpdated = "UserStatusUpdated";
    public const string UserRolesUpdated = "UserRolesUpdated";
    public const string RoleCreated = "RoleCreated";
    public const string RoleDeleted = "RoleDeleted";
    public const string PermissionsUpdated = "PermissionsUpdated";
    public const string SystemSettingsRetrievedSuccessfully = "SystemSettingsRetrievedSuccessfully";
    public const string SystemSettingsUpdatedSuccessfully = "SystemSettingsUpdatedSuccessfully";
    public const string EmailTemplatesRetrievedSuccessfully = "EmailTemplatesRetrievedSuccessfully";
    public const string EmailTemplateRetrievedSuccessfully = "EmailTemplateRetrievedSuccessfully";
    public const string EmailTemplateCreatedSuccessfully = "EmailTemplateCreatedSuccessfully";
    public const string EmailTemplateUpdatedSuccessfully = "EmailTemplateUpdatedSuccessfully";
    public const string EmailTemplateDeletedSuccessfully = "EmailTemplateDeletedSuccessfully";
    public const string EmailTemplateNotFound = "EmailTemplateNotFound";
    public const string EmailTemplateKeyAlreadyExists = "EmailTemplateKeyAlreadyExists";
    public const string EmailTemplateKeyRequired = "EmailTemplateKeyRequired";
    public const string EmailTemplateNameRequired = "EmailTemplateNameRequired";
    public const string EmailTemplateSubjectRequired = "EmailTemplateSubjectRequired";
    public const string EmailTemplateBodyRequired = "EmailTemplateBodyRequired";
    public const string EmailTemplateVersionMustBeGreaterThanZero = "EmailTemplateVersionMustBeGreaterThanZero";
    public const string EmailTemplateRequestedVersionNotFound = "EmailTemplateRequestedVersionNotFound";
    public const string EmailTemplateRollbackSuccessful = "EmailTemplateRollbackSuccessful";
    public const string EmailTemplateVersionsRetrievedSuccessfully = "EmailTemplateVersionsRetrievedSuccessfully";
    public const string EmailTemplateCoverageRetrievedSuccessfully = "EmailTemplateCoverageRetrievedSuccessfully";
    public const string EmailTemplateValid = "EmailTemplateValid";
    public const string EmailTemplateValidationErrors = "EmailTemplateValidationErrors";
    public const string EmailTemplateRecipientEmailRequired = "EmailTemplateRecipientEmailRequired";
    public const string EmailTemplateRecipientEmailInvalid = "EmailTemplateRecipientEmailInvalid";
    public const string EmailTemplateRenderFailed = "EmailTemplateRenderFailed";
    public const string EmailTemplateTestSendFailed = "EmailTemplateTestSendFailed";
    public const string EmailTemplateTestSentSuccessfully = "EmailTemplateTestSentSuccessfully";
    public const string EmailTemplateSourceAndTargetCultureSame = "EmailTemplateSourceAndTargetCultureSame";
    public const string EmailTemplateSourceTemplateNotFoundForCulture = "EmailTemplateSourceTemplateNotFoundForCulture";
    public const string EmailTemplateAutoTranslationDraftGeneratedSuccessfully = "EmailTemplateAutoTranslationDraftGeneratedSuccessfully";
    public const string EmailTemplateNoSourceTemplatesFoundForCulture = "EmailTemplateNoSourceTemplatesFoundForCulture";
    public const string EmailTemplateAutoTranslationDraftsGeneratedSuccessfully = "EmailTemplateAutoTranslationDraftsGeneratedSuccessfully";
    public const string EmailTemplateInvalidRequiredVariableNames = "EmailTemplateInvalidRequiredVariableNames";
    public const string EmailTemplateMissingRequiredVariables = "EmailTemplateMissingRequiredVariables";
    public const string EmailTemplateUndeclaredTemplateVariables = "EmailTemplateUndeclaredTemplateVariables";
    public const string EmailTemplateUnsafeContentDetected = "EmailTemplateUnsafeContentDetected";
    public const string EmailTemplateSubjectRenderError = "EmailTemplateSubjectRenderError";
    public const string EmailTemplateBodyRenderError = "EmailTemplateBodyRenderError";
    public const string RefreshTokenRequestRequired = "RefreshTokenRequestRequired";
    public const string LoginIsDisabled = "LoginIsDisabled";
    public const string RegistrationIsDisabled = "RegistrationIsDisabled";
    public const string GoogleLoginIsDisabled = "GoogleLoginIsDisabled";
    public const string ForgotPasswordIsDisabled = "ForgotPasswordIsDisabled";
    public const string PasswordChangeIsDisabled = "PasswordChangeIsDisabled";
    public const string SessionManagementIsDisabled = "SessionManagementIsDisabled";
    public const string MaintenanceModeEnabled = "MaintenanceModeEnabled";
    public const string PostmortemInvalidDateRange = "PostmortemInvalidDateRange";
    public const string PlatformConfigPackageInvalid = "PlatformConfigPackageInvalid";
    public const string PlatformConfigEntriesRequired = "PlatformConfigEntriesRequired";
    public const string PlatformConfigExported = "PlatformConfigExported";
    public const string PlatformConfigImported = "PlatformConfigImported";
    public const string PlatformPromotionApplied = "PlatformPromotionApplied";
    public const string ScheduledJobUnsupported = "ScheduledJobUnsupported";
    public const string ScheduledJobTriggered = "ScheduledJobTriggered";
    public const string MfaCodeRequired = "MfaCodeRequired";
    public const string MfaCodeInvalid = "MfaCodeInvalid";
    public const string MfaCodeSent = "MfaCodeSent";
    public const string MfaChallengeInvalid = "MfaChallengeInvalid";
    public const string MfaCodeDeliveryFailed = "MfaCodeDeliveryFailed";
    public const string MfaUpdated = "MfaUpdated";

    // Access Control Validation Messages
    public const string AccessControlPageNumberGreaterThanZero = "AccessControlPageNumberGreaterThanZero";
    public const string AccessControlPageSizeBetweenOneAndTwoHundred = "AccessControlPageSizeBetweenOneAndTwoHundred";
    public const string AccessControlSearchTermMaxLength = "AccessControlSearchTermMaxLength";
    public const string AccessControlRoleNameRequired = "AccessControlRoleNameRequired";
    public const string AccessControlRoleNameMaxLength = "AccessControlRoleNameMaxLength";
    public const string AccessControlUserIdRequired = "AccessControlUserIdRequired";
    public const string AccessControlRolesRequired = "AccessControlRolesRequired";
    public const string AccessControlAtLeastOneRoleRequired = "AccessControlAtLeastOneRoleRequired";
    public const string AccessControlRoleNameCannotBeEmpty = "AccessControlRoleNameCannotBeEmpty";
    public const string AccessControlPermissionsRequired = "AccessControlPermissionsRequired";
    public const string AccessControlPermissionCannotBeEmpty = "AccessControlPermissionCannotBeEmpty";
    public const string AccessControlPermissionMaxLength = "AccessControlPermissionMaxLength";

    // Compliance Messages
    public const string TermsRetrievedSuccessfully = "TermsRetrievedSuccessfully";
    public const string ActiveTermsRetrievedSuccessfully = "ActiveTermsRetrievedSuccessfully";
    public const string NoActiveTermsFoundForType = "NoActiveTermsFoundForType";
    public const string TermsNotFound = "TermsNotFound";
    public const string TermsCreatedSuccessfully = "TermsCreatedSuccessfully";
    public const string TermsUpdatedSuccessfully = "TermsUpdatedSuccessfully";
    public const string TermsDeletedSuccessfully = "TermsDeletedSuccessfully";
    public const string TermsActivatedSuccessfully = "TermsActivatedSuccessfully";
    public const string TermsAlreadyAccepted = "TermsAlreadyAccepted";
    public const string TermsAcceptedSuccessfully = "TermsAcceptedSuccessfully";
    public const string UserAcceptancesRetrievedSuccessfully = "UserAcceptancesRetrievedSuccessfully";
    public const string NoActiveTermsFound = "NoActiveTermsFound";
    public const string UserHasAcceptedLatestTerms = "UserHasAcceptedLatestTerms";
    public const string UserHasNotAcceptedLatestTerms = "UserHasNotAcceptedLatestTerms";
    public const string ComplianceReacceptanceEmailTitle = "ComplianceReacceptanceEmailTitle";
    public const string ComplianceReacceptanceEmailContent = "ComplianceReacceptanceEmailContent";
    public const string AgreementNameLabel = "AgreementNameLabel";
    public const string VersionLabel = "VersionLabel";
    public const string EffectiveDateLabel = "EffectiveDateLabel";
    public const string SummaryLabel = "SummaryLabel";
    public const string ReviewAgreementButton = "ReviewAgreementButton";

    // Localization Controller Messages
    public const string LanguageCodeRequired = "LanguageCodeRequired";
    public const string LocalizationKeyAlreadyExistsForCulture = "LocalizationKeyAlreadyExistsForCulture";
    public const string LocalizationCreateFailed = "LocalizationCreateFailed";
    public const string CultureRequired = "CultureRequired";
    public const string CultureDeletedSuccessfully = "CultureDeletedSuccessfully";
    public const string LocalizationCultureNotFound = "LocalizationCultureNotFound";
    public const string LocalizationLastCultureMustRemain = "LocalizationLastCultureMustRemain";
    public const string LocalizationNoResourcesFoundForCulture = "LocalizationNoResourcesFoundForCulture";
    public const string SourceAndTargetCulturesRequired = "SourceAndTargetCulturesRequired";
    public const string SourceAndTargetCulturesCannotBeSame = "SourceAndTargetCulturesCannotBeSame";
    public const string NoResourcesFoundForSourceCulture = "NoResourcesFoundForSourceCulture";
    public const string LocalizationCloneCompleted = "LocalizationCloneCompleted";
    public const string LocalizationPublishedSuccessfully = "LocalizationPublishedSuccessfully";
    public const string LocalizationResourceNotFound = "LocalizationResourceNotFound";

    // File Manager Messages
    public const string ExtractionFolderCreateEmptyResponse = "ExtractionFolderCreateEmptyResponse";
    public const string ExtractionFolderCreateFailed = "ExtractionFolderCreateFailed";
    public const string LoggedOutSuccessfully = "LoggedOutSuccessfully";
}
