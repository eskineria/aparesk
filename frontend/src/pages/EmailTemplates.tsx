import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import {
    Alert,
    Badge,
    Button,
    Card,
    Col,
    Container,
    Form,
    ListGroup,
    Modal,
    Row,
    Spinner,
    Table,
} from 'react-bootstrap'
import Editor from '@monaco-editor/react'
import type { editor as MonacoEditor } from 'monaco-editor'
import { LuBookOpen, LuExpand } from 'react-icons/lu'
import { useTranslation } from 'react-i18next'
import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import ConfirmationModal from '@/components/ConfirmationModal'
import EmailTemplateService from '@/services/emailTemplateService'
import localizationService from '@/services/localizationService'
import type {
    AutoTranslateCultureRequest,
    AutoTranslateEmailTemplateRequest,
    CreateEmailTemplateRequest,
    EmailTemplate,
    EmailTemplateCoverageItem,
    EmailTemplateRevision,
    EmailTemplateValidationResult,
    UpdateEmailTemplateRequest,
} from '@/types/emailTemplate'
import { showToast } from '@/utils/toast'

type TemplateFormState = {
    id: number | null
    key: string
    culture: string
    name: string
    subject: string
    body: string
    isActive: boolean
    isDraft: boolean
    currentVersion: number
    publishedVersion: number | null
}

const EMPTY_TEMPLATE_FORM: TemplateFormState = {
    id: null,
    key: '',
    culture: 'en-US',
    name: '',
    subject: '',
    body: '',
    isActive: true,
    isDraft: true,
    currentVersion: 1,
    publishedVersion: null,
}

const templateVariableRegex = /{{\s*([a-zA-Z0-9_]+)\s*}}/g
const EMAIL_BODY_EDITOR_HEIGHT = 420
const EMAIL_BODY_EDITOR_FULLSCREEN_HEIGHT = '72vh'
const SCRIBAN_SNIPPET_LABEL = '{{ }}'
type MonacoInstance = typeof import('monaco-editor')
type RollbackConfirmState = { version: number; publishNow: boolean } | null

const formatIndexedPlaceholders = (template: string, values: Array<string | number>) => {
    return template.replace(/\{(\d+)\}/g, (rawMatch, rawIndex: string) => {
        const index = Number(rawIndex)
        if (!Number.isInteger(index) || index < 0 || index >= values.length) {
            return rawMatch
        }

        return String(values[index])
    })
}

const buildTemplateSampleValues = (
    translate: (key: string) => string,
): Record<string, Record<string, string>> => {
    const sampleValue = (key: string) => {
        return translate(`identity.email_templates.sample_values.${key}`)
    }

    const sampleName = sampleValue('sample_name')
    const greeting = formatIndexedPlaceholders(
        sampleValue('email_greeting'),
        [sampleName],
    )
    const verificationExpiry = formatIndexedPlaceholders(
        sampleValue('verification_code_email_expiry'),
        [180],
    )

    return {
        VerifyEmailCode: {
            VerificationCodeEmailTitle: sampleValue('verification_code_email_title'),
            Greeting: greeting,
            VerificationCodeEmailContent: sampleValue('verification_code_email_content'),
            VerificationCodeLabel: sampleValue('verification_code_label'),
            VerificationCode: '742901',
            OpenVerificationPageButton: sampleValue('open_verification_page_button'),
            ConfirmPageUrl: 'https://app.eskineria.com/confirm-email?email=john@example.com',
            EmailSecurityNote: sampleValue('email_security_note'),
            VerificationCodeEmailExpiry: verificationExpiry,
            EmailTeam: sampleValue('email_team'),
            EmailFooterIgnore: sampleValue('email_footer_ignore'),
        },
        ResetPassword: {
            reset_link: 'https://app.eskineria.com/reset-password?token=sample-token',
            ResetPasswordEmailTitle: sampleValue('reset_password_email_title'),
            Greeting: greeting,
            ResetPasswordEmailContent: sampleValue('reset_password_email_content'),
            ResetPasswordButton: sampleValue('reset_password_button'),
            EmailSecurityNote: sampleValue('email_security_note'),
            ResetPasswordEmailExpiry: sampleValue('reset_password_email_expiry'),
            EmailSupportText: sampleValue('email_support_text'),
            EmailTeam: sampleValue('email_team'),
            EmailFooterIgnore: sampleValue('email_footer_ignore'),
        },
        Welcome: {
            WelcomeEmailTitle: sampleValue('welcome_email_title'),
            Greeting: greeting,
            WelcomeEmailContent: sampleValue('welcome_email_content'),
            VerifyEmailButton: sampleValue('verify_email_button'),
            confirm_link: 'https://app.eskineria.com/confirm-email?token=sample-token',
            EmailSupportText: sampleValue('email_support_text'),
            EmailTeam: sampleValue('email_team'),
            EmailFooterIgnore: sampleValue('email_footer_ignore'),
        },
        PasswordChangedAlert: {
            PasswordChangedAlertEmailTitle: sampleValue('password_changed_alert_email_title'),
            Greeting: greeting,
            PasswordChangedAlertEmailContent: sampleValue('password_changed_alert_email_content'),
            SecurityEventTimeLabel: sampleValue('security_event_time_label'),
            SecurityEventLocationLabel: sampleValue('security_event_location_label'),
            SecurityEventDeviceLabel: sampleValue('security_event_device_label'),
            EventDateTime: sampleValue('security_event_datetime_password_changed'),
            LoginLocation: '203.0.113.10',
            DeviceInfo: sampleValue('security_event_device_web'),
            AccountSecurityActionText: sampleValue('account_security_secure_text'),
            AccountSecurityActionLink: 'https://app.eskineria.com/users/profile',
            AccountSecurityButton: sampleValue('account_security_button'),
            EmailTeam: sampleValue('email_team'),
            EmailFooterIgnore: sampleValue('email_footer_ignore'),
        },
        LoginAlert: {
            LoginAlertEmailTitle: sampleValue('login_alert_email_title'),
            Greeting: greeting,
            LoginAlertEmailContent: sampleValue('login_alert_email_content'),
            SecurityEventTimeLabel: sampleValue('security_event_time_label'),
            SecurityEventLocationLabel: sampleValue('security_event_location_label'),
            SecurityEventDeviceLabel: sampleValue('security_event_device_label'),
            EventDateTime: sampleValue('security_event_datetime_login_alert'),
            LoginLocation: '198.51.100.20',
            DeviceInfo: sampleValue('security_event_device_mobile'),
            AccountSecurityActionText: sampleValue('account_security_review_text'),
            AccountSecurityActionLink: 'https://app.eskineria.com/users/profile',
            ReviewSecurityButton: sampleValue('review_security_button'),
            EmailTeam: sampleValue('email_team'),
            EmailFooterIgnore: sampleValue('email_footer_ignore'),
        },
    }
}

const parseVariablesFromInput = (value: string) => {
    return value
        .split(/[\n,]/)
        .map((item) => item.trim())
        .filter((item) => item.length > 0)
        .filter((item, index, all) => all.indexOf(item) === index)
}

const normalizeCultureCode = (value: string) => value.trim().toLowerCase()

const resolvePreferredCulture = (availableCultures: string[], candidates: Array<string | undefined>) => {
    for (const candidate of candidates) {
        if (!candidate) {
            continue
        }

        const normalizedCandidate = normalizeCultureCode(candidate)
        const exactMatch = availableCultures.find((culture) => normalizeCultureCode(culture) === normalizedCandidate)
        if (exactMatch) {
            return exactMatch
        }

        const candidatePrefix = normalizedCandidate.split('-')[0]
        if (!candidatePrefix) {
            continue
        }

        const prefixMatch = availableCultures.find((culture) => normalizeCultureCode(culture).split('-')[0] === candidatePrefix)
        if (prefixMatch) {
            return prefixMatch
        }
    }

    return availableCultures.find((culture) => normalizeCultureCode(culture) === 'en-us')
        ?? availableCultures[0]
        ?? 'en-US'
}

const getChangeSourceLabel = (t: (key: string) => string, source: string) => {
    const normalized = source.trim().toLowerCase()
    if (normalized === 'draftsave') {
        return t('identity.email_templates.change_source_draft_save')
    }
    if (normalized === 'publish') {
        return t('identity.email_templates.change_source_publish')
    }
    if (normalized === 'rollback') {
        return t('identity.email_templates.change_source_rollback')
    }
    if (normalized === 'autotranslate') {
        return t('identity.email_templates.change_source_auto_translate')
    }
    if (normalized === 'startupseed') {
        return t('identity.email_templates.change_source_startup_seed')
    }
    if (normalized === 'migration') {
        return t('identity.email_templates.change_source_migration')
    }

    return source
}

const renderTemplateWithSampleData = (template: string, values: Record<string, string>) => {
    return template.replace(templateVariableRegex, (_, rawKey: string) => {
        if (Object.prototype.hasOwnProperty.call(values, rawKey)) {
            return values[rawKey]
        }

        return `{{ ${rawKey} }}`
    })
}

const EmailTemplates = () => {
    const { t, i18n } = useTranslation()
    const i18nCultureSyncRef = useRef('')
    const [templates, setTemplates] = useState<EmailTemplate[]>([])
    const [versions, setVersions] = useState<EmailTemplateRevision[]>([])
    const [coverage, setCoverage] = useState<EmailTemplateCoverageItem[]>([])
    const [cultures, setCultures] = useState<string[]>([])

    const [selectedTemplateId, setSelectedTemplateId] = useState<number | null>(null)
    const [selectedCulture, setSelectedCulture] = useState<string>('')
    const [form, setForm] = useState<TemplateFormState>(EMPTY_TEMPLATE_FORM)
    const [requiredVariablesInput, setRequiredVariablesInput] = useState('')
    const [validationResult, setValidationResult] = useState<EmailTemplateValidationResult | null>(null)

    const [testEmail, setTestEmail] = useState('')
    const [usePublishedVersionForTest, setUsePublishedVersionForTest] = useState(true)

    const [autoTranslateSourceCulture, setAutoTranslateSourceCulture] = useState('en-US')
    const [autoTranslateTargetCulture, setAutoTranslateTargetCulture] = useState('tr-TR')
    const [autoTranslateOverwriteExisting, setAutoTranslateOverwriteExisting] = useState(false)

    const [batchSourceCulture, setBatchSourceCulture] = useState('en-US')
    const [batchTargetCulture, setBatchTargetCulture] = useState('tr-TR')

    const [isLoading, setIsLoading] = useState(true)
    const [isSaving, setIsSaving] = useState(false)
    const [isPublishing, setIsPublishing] = useState(false)
    const [isDeleting, setIsDeleting] = useState(false)
    const [isLoadingVersions, setIsLoadingVersions] = useState(false)
    const [isLoadingCoverage, setIsLoadingCoverage] = useState(false)
    const [isSendingTest, setIsSendingTest] = useState(false)
    const [isAutoTranslating, setIsAutoTranslating] = useState(false)
    const isGovernanceEnabled = true
    const isGovernanceReady = true
    const [showDocumentationModal, setShowDocumentationModal] = useState(false)
    const [showBodyEditorFullscreenModal, setShowBodyEditorFullscreenModal] = useState(false)
    const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)
    const [rollbackConfirmState, setRollbackConfirmState] = useState<RollbackConfirmState>(null)
    const monacoRef = useRef<MonacoInstance | null>(null)
    const completionProviderDisposableRef = useRef<{ dispose: () => void } | null>(null)

    const availableCultures = useMemo(() => {
        const fromTemplates = templates.map((item) => item.culture)
        return Array.from(new Set([...cultures, ...fromTemplates])).sort((a, b) => a.localeCompare(b))
    }, [cultures, templates])

    const requiredVariables = useMemo(() => {
        return parseVariablesFromInput(requiredVariablesInput)
    }, [requiredVariablesInput])

    const templateSampleValues = useMemo(() => {
        const previewCulture = selectedCulture || i18n.resolvedLanguage || i18n.language || form.culture || 'en-US'
        const translateForPreview = i18n.getFixedT(previewCulture)
        return buildTemplateSampleValues((key) => translateForPreview(key))
    }, [selectedCulture, form.culture, i18n, i18n.language, i18n.resolvedLanguage])

    const documentationSections = useMemo(() => ([
        {
            title: t('identity.email_templates.docs_section_library_title'),
            items: [
                {
                    label: t('identity.email_templates.list_title'),
                    description: t('identity.email_templates.docs_library_template_list_desc'),
                },
                {
                    label: t('identity.localization.culture'),
                    description: t('identity.email_templates.docs_library_culture_filter_desc'),
                },
            ],
        },
        {
            title: t('identity.email_templates.docs_section_editor_title'),
            items: [
                {
                    label: t('identity.email_templates.key_label'),
                    description: t('identity.email_templates.docs_editor_key_desc'),
                },
                {
                    label: t('identity.localization.culture'),
                    description: t('identity.email_templates.docs_editor_culture_desc'),
                },
                {
                    label: t('identity.email_templates.subject_label'),
                    description: t('identity.email_templates.docs_editor_subject_desc'),
                },
                {
                    label: t('identity.email_templates.required_variables'),
                    description: t('identity.email_templates.docs_editor_required_variables_desc'),
                },
                {
                    label: t('identity.email_templates.body_label'),
                    description: t('identity.email_templates.docs_editor_body_desc'),
                },
                {
                    label: t('identity.email_templates.validate_button'),
                    description: t('identity.email_templates.docs_editor_validate_desc'),
                },
                {
                    label: t('common.save_draft'),
                    description: t('identity.email_templates.docs_editor_save_draft_desc'),
                },
                {
                    label: t('identity.localization.publish'),
                    description: t('identity.email_templates.docs_editor_publish_desc'),
                },
                {
                    label: t('identity.email_templates.delete_button'),
                    description: t('identity.email_templates.docs_editor_delete_desc'),
                },
            ],
        },
        {
            title: t('identity.email_templates.docs_section_preview_title'),
            items: [
                {
                    label: t('identity.email_templates.preview_title'),
                    description: t('identity.email_templates.docs_preview_render_desc'),
                },
                {
                    label: t('identity.email_templates.available_variables'),
                    description: t('identity.email_templates.docs_preview_detected_variables_desc'),
                },
            ],
        },
        {
            title: t('identity.email_templates.docs_section_versioning_title'),
            items: [
                {
                    label: t('identity.email_templates.versioning'),
                    description: t('identity.email_templates.docs_version_history_desc'),
                },
                {
                    label: t('identity.email_templates.rollback_draft'),
                    description: t('identity.email_templates.docs_version_rollback_desc'),
                },
            ],
        },
        {
            title: t('identity.email_templates.docs_section_test_title'),
            items: [
                {
                    label: t('identity.email_templates.send_test'),
                    description: t('identity.email_templates.docs_test_send_desc'),
                },
                {
                    label: t('identity.email_templates.use_published_for_test'),
                    description: t('identity.email_templates.docs_test_published_toggle_desc'),
                },
            ],
        },
        {
            title: t('identity.email_templates.docs_section_translation_title'),
            items: [
                {
                    label: t('identity.email_templates.auto_translate_template'),
                    description: t('identity.email_templates.docs_translation_single_desc'),
                },
                {
                    label: t('identity.email_templates.auto_translate_all'),
                    description: t('identity.email_templates.docs_translation_batch_desc'),
                },
                {
                    label: t('identity.email_templates.overwrite_existing'),
                    description: t('identity.email_templates.docs_translation_overwrite_desc'),
                },
            ],
        },
        {
            title: t('identity.email_templates.docs_section_coverage_title'),
            items: [
                {
                    label: t('identity.email_templates.missing'),
                    description: t('identity.email_templates.docs_coverage_missing_desc'),
                },
                {
                    label: t('identity.localization.workflow_status.draft'),
                    description: t('identity.email_templates.docs_coverage_draft_desc'),
                },
            ],
        },
        {
            title: t('identity.email_templates.docs_section_governance_title'),
            items: [
                {
                    label: t('identity.email_templates.governance_disabled_short'),
                    description: t('identity.email_templates.docs_governance_desc'),
                },
            ],
        },
    ]), [t])

    const selectedVariables = useMemo(() => {
        const base = { ...(templateSampleValues[form.key] ?? {}) }
        for (const requiredVariable of requiredVariables) {
            if (!Object.prototype.hasOwnProperty.call(base, requiredVariable)) {
                base[requiredVariable] = `sample_${requiredVariable}`
            }
        }
        return base
    }, [templateSampleValues, form.key, requiredVariables])

    const detectedVariables = useMemo(() => {
        const variableSet = new Set<string>()
        const content = `${form.subject}\n${form.body}`
        for (const match of content.matchAll(templateVariableRegex)) {
            const variable = (match[1] ?? '').trim()
            if (variable) {
                variableSet.add(variable)
            }
        }

        return Array.from(variableSet).sort((a, b) => a.localeCompare(b))
    }, [form.subject, form.body])

    const completionVariables = useMemo(() => {
        const fromTemplate = Object.keys(templateSampleValues[form.key] ?? {})
        return Array.from(new Set([...requiredVariables, ...detectedVariables, ...fromTemplate]))
            .sort((left, right) => left.localeCompare(right))
    }, [requiredVariables, detectedVariables, templateSampleValues, form.key])

    const registerBodyEditorCompletions = useCallback(() => {
        const monaco = monacoRef.current
        if (!monaco) {
            return
        }

        completionProviderDisposableRef.current?.dispose()
        completionProviderDisposableRef.current = monaco.languages.registerCompletionItemProvider('html', {
            triggerCharacters: ['{', ' '],
            provideCompletionItems: (
                model: MonacoEditor.ITextModel,
                position: any,
            ) => {
                const wordUntil = model.getWordUntilPosition(position)
                const range = {
                    startLineNumber: position.lineNumber,
                    endLineNumber: position.lineNumber,
                    startColumn: wordUntil.startColumn,
                    endColumn: wordUntil.endColumn,
                }

                const variableSuggestions = completionVariables.map((variable, index) => ({
                    label: variable,
                    kind: monaco.languages.CompletionItemKind.Variable,
                    insertText: `{{ ${variable} }}`,
                    range,
                    detail: t('identity.email_templates.intellisense_variable_detail', 'Template variable'),
                    documentation: t(
                        'identity.email_templates.intellisense_variable_doc',
                        'Inserts a Scriban placeholder into the template body.',
                    ),
                    sortText: `a${index.toString().padStart(4, '0')}`,
                }))

                const snippetSuggestion = {
                    label: SCRIBAN_SNIPPET_LABEL,
                    kind: monaco.languages.CompletionItemKind.Snippet,
                    insertText: '{{ ${1:VariableName} }}',
                    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                    range,
                    detail: t('identity.email_templates.intellisense_placeholder_detail', 'Scriban placeholder'),
                    documentation: t(
                        'identity.email_templates.intellisense_placeholder_doc',
                        'Creates a placeholder in {{ VariableName }} format.',
                    ),
                    sortText: '0000',
                }

                return {
                    suggestions: [snippetSuggestion, ...variableSuggestions],
                }
            },
        })
    }, [completionVariables, t])

    const onBodyEditorMount = useCallback((
        _editor: MonacoEditor.IStandaloneCodeEditor,
        monaco: MonacoInstance,
    ) => {
        monacoRef.current = monaco
        registerBodyEditorCompletions()
    }, [registerBodyEditorCompletions])

    useEffect(() => {
        registerBodyEditorCompletions()
    }, [registerBodyEditorCompletions])

    useEffect(() => {
        return () => {
            completionProviderDisposableRef.current?.dispose()
            completionProviderDisposableRef.current = null
        }
    }, [])

    const previewSubject = useMemo(() => {
        return renderTemplateWithSampleData(form.subject, selectedVariables)
    }, [form.subject, selectedVariables])

    const previewBody = useMemo(() => {
        return renderTemplateWithSampleData(form.body, selectedVariables)
    }, [form.body, selectedVariables])


    const loadCoverage = async () => {
        if (!isGovernanceReady || !isGovernanceEnabled) {
            setCoverage([])
            return
        }

        setIsLoadingCoverage(true)
        try {
            const response = await EmailTemplateService.getCoverage()
            if (response.success && response.data) {
                setCoverage(response.data)
            }
        } catch {
            // axios interceptor handles toast
        } finally {
            setIsLoadingCoverage(false)
        }
    }

    const loadVersions = async (templateId: number | null) => {
        if (templateId == null) {
            setVersions([])
            return
        }

        if (!isGovernanceReady || !isGovernanceEnabled) {
            setVersions([])
            return
        }

        setIsLoadingVersions(true)
        try {
            const response = await EmailTemplateService.getVersions(templateId)
            if (response.success && response.data) {
                setVersions(response.data)
            }
        } catch {
            // axios interceptor handles toast
        } finally {
            setIsLoadingVersions(false)
        }
    }

    const applyTemplateToForm = (template: EmailTemplate | null) => {
        if (!template) {
            const defaultCulture = selectedCulture || i18n.resolvedLanguage || i18n.language || 'en-US'
            setSelectedTemplateId(null)
            setForm({
                ...EMPTY_TEMPLATE_FORM,
                culture: defaultCulture,
            })
            setRequiredVariablesInput('')
            setValidationResult(null)
            setVersions([])
            return
        }

        setSelectedTemplateId(template.id)
        setForm({
            id: template.id,
            key: template.key,
            culture: template.culture,
            name: template.name,
            subject: template.subject,
            body: template.body,
            isActive: template.isActive,
            isDraft: template.isDraft,
            currentVersion: template.currentVersion,
            publishedVersion: template.publishedVersion,
        })
        setRequiredVariablesInput(template.requiredVariables.join(', '))
        setValidationResult(null)

        setAutoTranslateSourceCulture(template.culture)
        if (autoTranslateTargetCulture === template.culture && availableCultures.length > 0) {
            const fallbackTarget = availableCultures.find((culture) => culture !== template.culture)
            if (fallbackTarget) {
                setAutoTranslateTargetCulture(fallbackTarget)
            }
        }
    }

    const loadTemplates = async (preferredTemplateId?: number | null, preferredTemplateKey?: string) => {
        setIsLoading(true)
        try {
            const requestedCulture = selectedCulture || undefined
            const response = await EmailTemplateService.getAll(requestedCulture)
            if (response.success && response.data) {
                if (requestedCulture && response.data.length === 0) {
                    setSelectedCulture('')
                    return
                }

                const nextTemplates = response.data
                setTemplates(nextTemplates)

                const mergedCultures = Array.from(
                    new Set([...cultures, ...nextTemplates.map((item) => item.culture)]),
                ).sort((a, b) => a.localeCompare(b))
                setCultures(mergedCultures)

                if (nextTemplates.length === 0) {
                    applyTemplateToForm(null)
                    return
                }

                const targetId = preferredTemplateId ?? selectedTemplateId
                const selectedById = targetId == null
                    ? undefined
                    : nextTemplates.find((item) => item.id === targetId)

                const normalizedPreferredKey = (preferredTemplateKey ?? form.key).trim().toLowerCase()
                const availableTemplateCultures = Array.from(new Set(nextTemplates.map((item) => item.culture)))
                const preferredTemplateCulture = resolvePreferredCulture(availableTemplateCultures, [
                    selectedCulture,
                    i18n.language,
                    i18n.resolvedLanguage,
                    form.culture,
                ])
                const preferredTemplateCulturePrefix = normalizeCultureCode(preferredTemplateCulture).split('-')[0]

                const selectedByKey = normalizedPreferredKey.length === 0
                    ? undefined
                    : (() => {
                        const sameKeyTemplates = nextTemplates.filter(
                            (item) => item.key.trim().toLowerCase() === normalizedPreferredKey,
                        )

                        if (sameKeyTemplates.length === 0) {
                            return undefined
                        }

                        const exactCultureMatch = sameKeyTemplates.find(
                            (item) => normalizeCultureCode(item.culture) === normalizeCultureCode(preferredTemplateCulture),
                        )
                        if (exactCultureMatch) {
                            return exactCultureMatch
                        }

                        const prefixCultureMatch = sameKeyTemplates.find(
                            (item) => normalizeCultureCode(item.culture).split('-')[0] === preferredTemplateCulturePrefix,
                        )
                        if (prefixCultureMatch) {
                            return prefixCultureMatch
                        }

                        return sameKeyTemplates[0]
                    })()

                const selectedTemplate = preferredTemplateId != null
                    ? (selectedById ?? selectedByKey ?? nextTemplates[0])
                    : (selectedByKey ?? selectedById ?? nextTemplates[0])
                applyTemplateToForm(selectedTemplate)
                await loadVersions(selectedTemplate.id)
            }
        } catch {
            // axios interceptor handles toast
        } finally {
            setIsLoading(false)
        }
    }

    useEffect(() => {
        const bootstrap = async () => {
            try {
                const fetchedCultures = await localizationService.getCultures()
                const normalizedCultures = Array.from(new Set(fetchedCultures)).sort((a, b) => a.localeCompare(b))
                setCultures(normalizedCultures)

                const preferredCulture = resolvePreferredCulture(normalizedCultures, [
                    i18n.language,
                    i18n.resolvedLanguage,
                ])

                setSelectedCulture(preferredCulture)
                setForm((prev) => ({ ...prev, culture: preferredCulture }))
                setAutoTranslateSourceCulture(preferredCulture)
                setBatchSourceCulture(preferredCulture)
                const fallbackTarget = normalizedCultures.find((culture) => culture !== preferredCulture) ?? preferredCulture
                setAutoTranslateTargetCulture(fallbackTarget)
                setBatchTargetCulture(fallbackTarget)
            } catch {
                const fallbackCultures = ['en-US', 'tr-TR']
                setCultures(fallbackCultures)
                const preferredCulture = resolvePreferredCulture(fallbackCultures, [
                    i18n.language,
                    i18n.resolvedLanguage,
                ])
                setSelectedCulture(preferredCulture)
                setForm((prev) => ({ ...prev, culture: preferredCulture }))
            }
        }

        void bootstrap()
    }, [])

    useEffect(() => {
        void loadTemplates(undefined, form.key)
    }, [selectedCulture])

    useEffect(() => {
        if (!isGovernanceReady) {
            return
        }

        if (!isGovernanceEnabled) {
            setCoverage([])
            setVersions([])
            return
        }

        void loadCoverage()
    }, [isGovernanceReady, isGovernanceEnabled])

    useEffect(() => {
        if (!isGovernanceReady || !isGovernanceEnabled || selectedTemplateId == null) {
            return
        }

        void loadVersions(selectedTemplateId)
    }, [isGovernanceReady, isGovernanceEnabled, selectedTemplateId])

    useEffect(() => {
        if (availableCultures.length === 0) {
            return
        }

        const languageSignature = `${i18n.language}|${i18n.resolvedLanguage || ''}`
        if (i18nCultureSyncRef.current === languageSignature) {
            return
        }

        i18nCultureSyncRef.current = languageSignature
        const preferredCulture = resolvePreferredCulture(availableCultures, [
            i18n.language,
            i18n.resolvedLanguage,
        ])

        setSelectedCulture(preferredCulture)
        setForm((prev) => prev.id == null ? { ...prev, culture: preferredCulture } : prev)
    }, [availableCultures, i18n.language, i18n.resolvedLanguage])

    const onCreateNew = () => {
        applyTemplateToForm(null)
    }

    const onSelectTemplate = async (template: EmailTemplate) => {
        applyTemplateToForm(template)
        await loadVersions(template.id)
    }

    const onValidate = async () => {
        if (!isGovernanceEnabled) {
            showToast(t('identity.email_templates.governance_disabled_hint'), 'warning')
            return
        }

        try {
            const response = await EmailTemplateService.validate({
                subject: form.subject,
                body: form.body,
                requiredVariables,
            })

            if (response.success && response.data) {
                setValidationResult(response.data)
                if (response.data.isValid) {
                    showToast(t('common.success'), 'success')
                } else {
                    showToast(t('common.validationError'), 'warning')
                }
            }
        } catch {
            // axios interceptor handles toast
        }
    }

    const onSave = async (publishNow = false) => {
        if (publishNow && !isGovernanceEnabled) {
            showToast(t('identity.email_templates.governance_disabled_hint'), 'warning')
            return
        }

        setIsSaving(true)
        try {
            if (form.id == null) {
                const payload: CreateEmailTemplateRequest = {
                    key: form.key.trim(),
                    culture: form.culture.trim(),
                    name: form.name.trim(),
                    subject: form.subject.trim(),
                    body: form.body,
                    requiredVariables,
                    isActive: form.isActive,
                    publishNow,
                }

                const response = await EmailTemplateService.create(payload)
                if (response.success && response.data) {
                    showToast(t('identity.email_templates.create_success'), 'success')

                    if (selectedCulture && selectedCulture !== payload.culture) {
                        setSelectedCulture(payload.culture)
                    }

                    await loadTemplates(response.data.id)
                    await loadCoverage()
                }

                return
            }

            const payload: UpdateEmailTemplateRequest = {
                name: form.name.trim(),
                subject: form.subject.trim(),
                body: form.body,
                requiredVariables,
                isActive: form.isActive,
                publishNow,
            }

            const response = await EmailTemplateService.update(form.id, payload)
            if (response.success && response.data) {
                showToast(t('identity.email_templates.save_success'), 'success')
                await loadTemplates(response.data.id)
                await loadCoverage()
            }
        } catch {
            // axios interceptor handles toast
        } finally {
            setIsSaving(false)
        }
    }

    const onPublish = async () => {
        if (!isGovernanceEnabled) {
            showToast(t('identity.email_templates.governance_disabled_hint'), 'warning')
            return
        }

        if (form.id == null) {
            await onSave(true)
            return
        }

        setIsPublishing(true)
        try {
            const response = await EmailTemplateService.publish(form.id, { markActive: true })
            if (response.success && response.data) {
                showToast(t('identity.email_templates.publish_success'), 'success')
                await loadTemplates(response.data.id)
                await loadCoverage()
            }
        } catch {
            // axios interceptor handles toast
        } finally {
            setIsPublishing(false)
        }
    }

    const onDelete = async () => {
        if (form.id == null) {
            return
        }

        setIsDeleting(true)
        try {
            const response = await EmailTemplateService.delete(form.id)
            if (response.success) {
                showToast(t('identity.email_templates.delete_success'), 'success')
                await loadTemplates(null)
                await loadCoverage()
            }
        } catch {
            // axios interceptor handles toast
        } finally {
            setIsDeleting(false)
            setShowDeleteConfirm(false)
        }
    }

    const onRollback = async (version: number, publishNow = false) => {
        if (!isGovernanceEnabled) {
            showToast(t('identity.email_templates.governance_disabled_hint'), 'warning')
            return
        }

        if (form.id == null) {
            return
        }

        setIsSaving(true)
        try {
            const response = await EmailTemplateService.rollback(form.id, { version, publishNow })
            if (response.success && response.data) {
                showToast(t('identity.email_templates.rollback_success'), 'success')
                await loadTemplates(response.data.id)
                await loadCoverage()
            }
        } catch {
            // axios interceptor handles toast
        } finally {
            setIsSaving(false)
            setRollbackConfirmState(null)
        }
    }

    const onSendTest = async () => {
        if (!testEmail.trim()) {
            showToast(t('identity.email_templates.test_email_required'), 'warning')
            return
        }

        setIsSendingTest(true)
        try {
            const response = await EmailTemplateService.sendTest({
                templateId: form.id ?? undefined,
                key: form.id == null ? form.key : undefined,
                culture: form.culture,
                toEmail: testEmail.trim(),
                usePublishedVersion: usePublishedVersionForTest,
            })

            if (response.success) {
                showToast(t('identity.email_templates.test_email_sent'), 'success')
            }
        } catch {
            // axios interceptor handles toast
        } finally {
            setIsSendingTest(false)
        }
    }

    const onAutoTranslateTemplate = async () => {
        if (!form.key.trim()) {
            showToast(t('identity.email_templates.template_key_required_warning'), 'warning')
            return
        }

        const payload: AutoTranslateEmailTemplateRequest = {
            key: form.key.trim(),
            sourceCulture: autoTranslateSourceCulture,
            targetCulture: autoTranslateTargetCulture,
            overwriteExisting: autoTranslateOverwriteExisting,
        }

        setIsAutoTranslating(true)
        try {
            const response = await EmailTemplateService.autoTranslate(payload)
            if (response.success && response.data) {
                showToast(
                    t('identity.email_templates.auto_translate_summary', {
                        created: response.data.createdCount,
                        updated: response.data.updatedCount,
                    }),
                    'success',
                )
                await loadCoverage()
                if (selectedCulture === payload.targetCulture) {
                    await loadTemplates()
                }
            }
        } catch {
            // axios interceptor handles toast
        } finally {
            setIsAutoTranslating(false)
        }
    }

    const onAutoTranslateCulture = async () => {
        const payload: AutoTranslateCultureRequest = {
            sourceCulture: batchSourceCulture,
            targetCulture: batchTargetCulture,
            overwriteExisting: autoTranslateOverwriteExisting,
        }

        setIsAutoTranslating(true)
        try {
            const response = await EmailTemplateService.autoTranslateCulture(payload)
            if (response.success && response.data) {
                showToast(
                    t('identity.email_templates.auto_translate_summary', {
                        created: response.data.createdCount,
                        updated: response.data.updatedCount,
                    }),
                    'success',
                )
                await loadCoverage()
                if (selectedCulture === payload.targetCulture) {
                    await loadTemplates()
                }
            }
        } catch {
            // axios interceptor handles toast
        } finally {
            setIsAutoTranslating(false)
        }
    }

    useEffect(() => {
        if (!showBodyEditorFullscreenModal) {
            return
        }

        const handleKeyboardShortcut = (event: KeyboardEvent) => {
            const pressedKey = event.key.toLowerCase()

            if ((event.ctrlKey || event.metaKey) && pressedKey === 's') {
                event.preventDefault()

                if (!isSaving && !isDeleting && !isPublishing) {
                    void onSave(false)
                }

                return
            }

            if (pressedKey === 'escape') {
                event.preventDefault()
                setShowBodyEditorFullscreenModal(false)
            }
        }

        window.addEventListener('keydown', handleKeyboardShortcut)
        return () => {
            window.removeEventListener('keydown', handleKeyboardShortcut)
        }
    }, [
        showBodyEditorFullscreenModal,
        isSaving,
        isDeleting,
        isPublishing,
        onSave,
    ])

    return (
        <VerticalLayout>
            <PageBreadcrumb title={t('identity.email_templates.title')} subtitle={t('identity.title')} />
            <Container fluid>
                <div className="d-flex justify-content-end mb-3">
                    <Button
                        variant="outline-secondary"
                        size="sm"
                        className="d-inline-flex align-items-center gap-2"
                        onClick={() => setShowDocumentationModal(true)}
                        title={t('identity.email_templates.documentation_title')}>
                        <LuBookOpen size={16} />
                        <span>{t('identity.email_templates.documentation_button', 'Open Docs')}</span>
                    </Button>
                </div>

                {isGovernanceReady && !isGovernanceEnabled && (
                    <Alert variant="warning" className="mb-3">
                        {t('identity.email_templates.governance_disabled_hint')}
                    </Alert>
                )}

                <Row className="g-3">
                    <Col xl={3}>
                        <Card className="h-100">
                            <Card.Body>
                                <div className="d-flex align-items-center justify-content-between mb-3">
                                    <h5 className="mb-0">{t('identity.email_templates.list_title')}</h5>
                                    <Button variant="primary" size="sm" onClick={onCreateNew}>
                                        {t('identity.email_templates.new_button')}
                                    </Button>
                                </div>

                                <div className="mb-3">
                                    <Form.Label>{t('identity.localization.culture')}</Form.Label>
                                    <Form.Select
                                        value={selectedCulture}
                                        onChange={(event) => setSelectedCulture(event.target.value)}>
                                        <option value="">
                                            {t('identity.localization.all_cultures')}
                                        </option>
                                        {availableCultures.map((culture) => (
                                            <option key={culture} value={culture}>
                                                {culture}
                                            </option>
                                        ))}
                                    </Form.Select>
                                </div>

                                {isLoading ? (
                                    <div className="d-flex align-items-center gap-2 text-muted">
                                        <Spinner animation="border" size="sm" />
                                        <span>{t('common.loading')}</span>
                                    </div>
                                ) : (
                                    <ListGroup>
                                        {templates.map((template) => (
                                            <ListGroup.Item
                                                key={template.id}
                                                action
                                                active={selectedTemplateId === template.id}
                                                onClick={() => void onSelectTemplate(template)}
                                                className="d-flex align-items-center justify-content-between">
                                                <div>
                                                    <div className="fw-semibold">{template.name}</div>
                                                    <div className="small opacity-75">{template.key}</div>
                                                    <div className="small opacity-75">{template.culture}</div>
                                                </div>
                                                <div className="d-flex flex-column gap-1 align-items-end">
                                                    <Badge bg={template.isActive ? 'success' : 'secondary'}>
                                                        {template.isActive
                                                            ? t('identity.email_templates.active_badge')
                                                            : t('identity.email_templates.inactive_badge')}
                                                    </Badge>
                                                    <Badge bg={template.isDraft ? 'warning' : 'primary'}>
                                                        {template.isDraft
                                                            ? t('identity.localization.workflow_status.draft')
                                                            : t('identity.localization.workflow_status.published')}
                                                    </Badge>
                                                </div>
                                            </ListGroup.Item>
                                        ))}
                                    </ListGroup>
                                )}
                            </Card.Body>
                        </Card>
                    </Col>

                    <Col xl={6}>
                        <Card className="mb-3">
                            <Card.Body>
                                <h5 className="mb-3">{t('identity.email_templates.editor_title')}</h5>
                                <Row className="g-3">
                                    <Col md={6}>
                                        <Form.Label>{t('identity.email_templates.key_label')}</Form.Label>
                                        <Form.Control
                                            value={form.key}
                                            onChange={(event) =>
                                                setForm((prev) => ({ ...prev, key: event.target.value }))
                                            }
                                            readOnly={form.id != null}
                                        />
                                    </Col>
                                    <Col md={6}>
                                        <Form.Label>{t('identity.localization.culture')}</Form.Label>
                                        <Form.Select
                                            value={form.culture}
                                            onChange={(event) =>
                                                setForm((prev) => ({ ...prev, culture: event.target.value }))
                                            }
                                            disabled={form.id != null}>
                                            {availableCultures.map((culture) => (
                                                <option key={culture} value={culture}>
                                                    {culture}
                                                </option>
                                            ))}
                                        </Form.Select>
                                    </Col>

                                    <Col md={12}>
                                        <Form.Label>{t('identity.email_templates.name_label')}</Form.Label>
                                        <Form.Control
                                            value={form.name}
                                            onChange={(event) =>
                                                setForm((prev) => ({ ...prev, name: event.target.value }))
                                            }
                                        />
                                    </Col>

                                    <Col md={12}>
                                        <Form.Label>{t('identity.email_templates.subject_label')}</Form.Label>
                                        <Form.Control
                                            value={form.subject}
                                            onChange={(event) =>
                                                setForm((prev) => ({ ...prev, subject: event.target.value }))
                                            }
                                        />
                                    </Col>

                                    <Col md={12}>
                                        <Form.Check
                                            type="switch"
                                            label={t('identity.email_templates.active_label')}
                                            checked={form.isActive}
                                            onChange={(event) =>
                                                setForm((prev) => ({ ...prev, isActive: event.target.checked }))
                                            }
                                        />
                                    </Col>

                                    <Col md={12}>
                                        <Form.Label>{t('identity.email_templates.required_variables')}</Form.Label>
                                        <Form.Control
                                            value={requiredVariablesInput}
                                            onChange={(event) => setRequiredVariablesInput(event.target.value)}
                                            placeholder={t('identity.email_templates.required_variables_placeholder')}
                                        />
                                        <Form.Text className="text-muted">
                                            {t('identity.email_templates.required_variables_help')}
                                        </Form.Text>
                                    </Col>

                                    <Col md={12}>
                                        <div className="d-flex align-items-center justify-content-between mb-2">
                                            <Form.Label className="mb-0">{t('identity.email_templates.body_label')}</Form.Label>
                                            <Button
                                                variant="outline-secondary"
                                                size="sm"
                                                className="d-inline-flex align-items-center gap-1"
                                                onClick={() => setShowBodyEditorFullscreenModal(true)}>
                                                <LuExpand size={14} />
                                                <span>{t('identity.email_templates.fullscreen_button', 'Full Screen')}</span>
                                            </Button>
                                        </div>
                                        <div
                                            style={{
                                                border: '1px solid #dee2e6',
                                                borderRadius: 8,
                                                overflow: 'hidden',
                                            }}>
                                            <Editor
                                                height={EMAIL_BODY_EDITOR_HEIGHT}
                                                defaultLanguage="html"
                                                language="html"
                                                theme="vs-dark"
                                                value={form.body}
                                                path={`email-template-${form.id ?? 'draft'}-${form.culture}.html`}
                                                onMount={onBodyEditorMount}
                                                onChange={(value) =>
                                                    setForm((prev) => ({ ...prev, body: value ?? '' }))
                                                }
                                                options={{
                                                    minimap: { enabled: false },
                                                    wordWrap: 'on',
                                                    scrollBeyondLastLine: false,
                                                    automaticLayout: true,
                                                    fontSize: 13,
                                                    lineHeight: 22,
                                                    tabSize: 2,
                                                    insertSpaces: true,
                                                    formatOnPaste: true,
                                                    formatOnType: true,
                                                    padding: { top: 12, bottom: 12 },
                                                    renderWhitespace: 'selection',
                                                    fontFamily: "'Fira Code', 'Cascadia Code', monospace",
                                                    quickSuggestions: {
                                                        other: true,
                                                        comments: false,
                                                        strings: true,
                                                    },
                                                    suggestOnTriggerCharacters: true,
                                                    snippetSuggestions: 'top',
                                                    tabCompletion: 'on',
                                                }}
                                            />
                                        </div>
                                        <Form.Text className="text-muted">
                                            {t('identity.email_templates.body_help')} {' '}
                                            {t('identity.email_templates.body_intellisense_hint', 'Press Ctrl+Space for IntelliSense suggestions.')}
                                        </Form.Text>
                                    </Col>

                                    {validationResult && !validationResult.isValid && (
                                        <Col md={12}>
                                            <Alert variant="warning" className="mb-0">
                                                <div className="fw-semibold mb-1">
                                                    {t('common.validationError')}
                                                </div>
                                                <ul className="mb-0 ps-3">
                                                    {validationResult.errors.map((error, index) => (
                                                        <li key={`${error}-${index}`}>{error}</li>
                                                    ))}
                                                </ul>
                                            </Alert>
                                        </Col>
                                    )}

                                    <Col md={12}>
                                        <div className="d-flex gap-2 justify-content-end flex-wrap">
                                            <Button
                                                variant="outline-secondary"
                                                disabled={isSaving || isDeleting || isPublishing || !isGovernanceEnabled}
                                                onClick={() => void onValidate()}>
                                                {t('identity.email_templates.validate_button')}
                                            </Button>

                                            {form.id != null && (
                                                <Button
                                                    variant="outline-danger"
                                                    disabled={isDeleting || isSaving || isPublishing}
                                                    onClick={() => setShowDeleteConfirm(true)}>
                                                    {isDeleting ? t('common.loading') : t('identity.email_templates.delete_button')}
                                                </Button>
                                            )}

                                            <Button
                                                variant="outline-primary"
                                                disabled={isSaving || isDeleting || isPublishing}
                                                onClick={() => void onSave(false)}>
                                                {isSaving ? t('common.loading') : t('common.save_draft')}
                                            </Button>

                                            <Button
                                                disabled={isSaving || isDeleting || isPublishing || !isGovernanceEnabled}
                                                onClick={() => void onPublish()}>
                                                {isPublishing ? t('common.loading') : t('identity.localization.publish')}
                                            </Button>
                                        </div>
                                    </Col>
                                </Row>
                            </Card.Body>
                        </Card>

                        <Card>
                            <Card.Body>
                                <h5 className="mb-2">{t('identity.email_templates.preview_title')}</h5>
                                <p className="text-muted mb-3">
                                    {t('identity.email_templates.preview_subject')}: <strong>{previewSubject}</strong>
                                </p>

                                <div className="mb-3">
                                    <div className="fw-semibold mb-2">
                                        {t('identity.email_templates.available_variables')}
                                    </div>
                                    <div className="d-flex flex-wrap gap-2">
                                        {detectedVariables.length === 0 ? (
                                            <span className="text-muted">{t('identity.email_templates.no_variables')}</span>
                                        ) : (
                                            detectedVariables.map((variable) => (
                                                <Badge key={variable} bg="light" text="dark">
                                                    {`{{ ${variable} }}`}
                                                </Badge>
                                            ))
                                        )}
                                    </div>
                                </div>

                                <iframe
                                    title={t('identity.email_templates.preview_title')}
                                    srcDoc={previewBody}
                                    sandbox="allow-same-origin"
                                    style={{
                                        width: '100%',
                                        minHeight: 500,
                                        border: '1px solid #dee2e6',
                                        borderRadius: 8,
                                        background: '#fff',
                                    }}
                                />
                            </Card.Body>
                        </Card>
                    </Col>

                    <Col xl={3}>
                        <Card className="mb-3">
                            <Card.Body>
                                <h5 className="mb-3">{t('identity.email_templates.versioning')}</h5>
                                <div className="small text-muted mb-2">
                                    {t('identity.email_templates.current_version')}: {form.currentVersion}
                                </div>
                                <div className="small text-muted mb-3">
                                    {t('identity.email_templates.published_version')}:{' '}
                                    {form.publishedVersion ?? '-'}
                                </div>

                                {!isGovernanceEnabled ? (
                                    <div className="text-muted small">
                                        {t('identity.email_templates.governance_disabled_short')}
                                    </div>
                                ) : isLoadingVersions ? (
                                    <div className="d-flex align-items-center gap-2 text-muted">
                                        <Spinner animation="border" size="sm" />
                                        <span>{t('common.loading')}</span>
                                    </div>
                                ) : versions.length === 0 ? (
                                    <div className="text-muted small">{t('identity.email_templates.no_versions')}</div>
                                ) : (
                                    <ListGroup>
                                        {versions.map((version) => (
                                            <ListGroup.Item key={version.id}>
                                                <div className="d-flex align-items-center justify-content-between mb-2">
                                                    <div className="fw-semibold">v{version.version}</div>
                                                    {version.isPublishedSnapshot && (
                                                        <Badge bg="primary">{t('identity.localization.workflow_status.published')}</Badge>
                                                    )}
                                                </div>
                                                <div className="small text-muted mb-2">
                                                    {getChangeSourceLabel(t, version.changeSource)}
                                                </div>
                                                <div className="d-flex gap-2">
                                                    <Button
                                                        variant="outline-secondary"
                                                        size="sm"
                                                        disabled={isSaving || isPublishing || form.id == null || !isGovernanceEnabled}
                                                        onClick={() => setRollbackConfirmState({ version: version.version, publishNow: false })}>
                                                        {t('identity.email_templates.rollback_draft')}
                                                    </Button>
                                                    <Button
                                                        variant="outline-primary"
                                                        size="sm"
                                                        disabled={isSaving || isPublishing || form.id == null || !isGovernanceEnabled}
                                                        onClick={() => setRollbackConfirmState({ version: version.version, publishNow: true })}>
                                                        {t('identity.email_templates.rollback_publish')}
                                                    </Button>
                                                </div>
                                            </ListGroup.Item>
                                        ))}
                                    </ListGroup>
                                )}
                            </Card.Body>
                        </Card>

                        <Card className="mb-3">
                            <Card.Body>
                                <h5 className="mb-3">{t('identity.email_templates.test_send')}</h5>
                                <Form.Group className="mb-2">
                                    <Form.Label>{t('identity.email_templates.test_email')}</Form.Label>
                                    <Form.Control
                                        value={testEmail}
                                        onChange={(event) => setTestEmail(event.target.value)}
                                        placeholder={t('identity.email_templates.test_email_placeholder')}
                                    />
                                </Form.Group>
                                <Form.Check
                                    className="mb-3"
                                    type="checkbox"
                                    checked={usePublishedVersionForTest}
                                    onChange={(event) => setUsePublishedVersionForTest(event.target.checked)}
                                    label={t('identity.email_templates.use_published_for_test')}
                                />
                                <Button
                                    className="w-100"
                                    variant="outline-primary"
                                    disabled={isSendingTest || (!form.id && !form.key)}
                                    onClick={() => void onSendTest()}>
                                    {isSendingTest ? t('common.loading') : t('identity.email_templates.send_test')}
                                </Button>
                            </Card.Body>
                        </Card>

                        <Card className="mb-3">
                            <Card.Body>
                                <h5 className="mb-3">{t('identity.email_templates.auto_translate')}</h5>

                                <Form.Group className="mb-2">
                                    <Form.Label>{t('identity.localization.source_culture')}</Form.Label>
                                    <Form.Select
                                        value={autoTranslateSourceCulture}
                                        onChange={(event) => setAutoTranslateSourceCulture(event.target.value)}>
                                        {availableCultures.map((culture) => (
                                            <option key={`single-source-${culture}`} value={culture}>
                                                {culture}
                                            </option>
                                        ))}
                                    </Form.Select>
                                </Form.Group>
                                <Form.Group className="mb-2">
                                    <Form.Label>{t('identity.localization.target_culture')}</Form.Label>
                                    <Form.Select
                                        value={autoTranslateTargetCulture}
                                        onChange={(event) => setAutoTranslateTargetCulture(event.target.value)}>
                                        {availableCultures.map((culture) => (
                                            <option key={`single-target-${culture}`} value={culture}>
                                                {culture}
                                            </option>
                                        ))}
                                    </Form.Select>
                                </Form.Group>
                                <Form.Check
                                    className="mb-3"
                                    type="checkbox"
                                    checked={autoTranslateOverwriteExisting}
                                    onChange={(event) => setAutoTranslateOverwriteExisting(event.target.checked)}
                                    label={t('identity.email_templates.overwrite_existing')}
                                />
                                <Button
                                    className="w-100 mb-2"
                                    variant="outline-primary"
                                    disabled={isAutoTranslating || !form.key}
                                    onClick={() => void onAutoTranslateTemplate()}>
                                    {t('identity.email_templates.auto_translate_template')}
                                </Button>

                                <hr />

                                <Form.Group className="mb-2">
                                    <Form.Label>{t('identity.localization.source_culture')}</Form.Label>
                                    <Form.Select
                                        value={batchSourceCulture}
                                        onChange={(event) => setBatchSourceCulture(event.target.value)}>
                                        {availableCultures.map((culture) => (
                                            <option key={`batch-source-${culture}`} value={culture}>
                                                {culture}
                                            </option>
                                        ))}
                                    </Form.Select>
                                </Form.Group>
                                <Form.Group className="mb-3">
                                    <Form.Label>{t('identity.localization.target_culture')}</Form.Label>
                                    <Form.Select
                                        value={batchTargetCulture}
                                        onChange={(event) => setBatchTargetCulture(event.target.value)}>
                                        {availableCultures.map((culture) => (
                                            <option key={`batch-target-${culture}`} value={culture}>
                                                {culture}
                                            </option>
                                        ))}
                                    </Form.Select>
                                </Form.Group>

                                <Button
                                    className="w-100"
                                    variant="outline-secondary"
                                    disabled={isAutoTranslating}
                                    onClick={() => void onAutoTranslateCulture()}>
                                    {isAutoTranslating
                                        ? t('common.loading')
                                        : t('identity.email_templates.auto_translate_all')}
                                </Button>
                            </Card.Body>
                        </Card>

                        <Card>
                            <Card.Body>
                                <h5 className="mb-3">{t('identity.email_templates.translation_coverage')}</h5>
                                {!isGovernanceEnabled ? (
                                    <div className="text-muted small">
                                        {t('identity.email_templates.governance_disabled_short')}
                                    </div>
                                ) : isLoadingCoverage ? (
                                    <div className="d-flex align-items-center gap-2 text-muted">
                                        <Spinner animation="border" size="sm" />
                                        <span>{t('common.loading')}</span>
                                    </div>
                                ) : coverage.length === 0 ? (
                                    <div className="text-muted small">
                                        {t('identity.email_templates.no_coverage')}
                                    </div>
                                ) : (
                                    <Table responsive size="sm" className="align-middle mb-0">
                                        <thead>
                                            <tr>
                                                <th>{t('identity.email_templates.key_label')}</th>
                                                <th>{t('identity.email_templates.missing')}</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {coverage.map((item) => (
                                                <tr key={item.key}>
                                                    <td>
                                                        <div className="fw-semibold">{item.key}</div>
                                                        {item.draftCultureCodes.length > 0 && (
                                                            <div className="small text-warning">
                                                                {t('identity.localization.workflow_status.draft')}:{' '}
                                                                {item.draftCultureCodes.join(', ')}
                                                            </div>
                                                        )}
                                                    </td>
                                                    <td>
                                                        {item.missingCultures > 0 ? (
                                                            <div className="small text-danger">
                                                                {item.missingCultureCodes.join(', ')}
                                                            </div>
                                                        ) : (
                                                            <span className="text-success">0</span>
                                                        )}
                                                    </td>
                                                </tr>
                                            ))}
                                        </tbody>
                                    </Table>
                                )}
                            </Card.Body>
                        </Card>
                    </Col>
                </Row>
            </Container>

            <ConfirmationModal
                show={showDeleteConfirm}
                onHide={() => setShowDeleteConfirm(false)}
                onConfirm={() => void onDelete()}
                title={t('common.confirmAction')}
                message={t('identity.email_templates.delete_confirm')}
                variant="danger"
                confirmText={t('common.delete')}
                isLoading={isDeleting}
            />

            <ConfirmationModal
                show={rollbackConfirmState != null}
                onHide={() => setRollbackConfirmState(null)}
                onConfirm={() => {
                    if (!rollbackConfirmState) {
                        return
                    }

                    void onRollback(rollbackConfirmState.version, rollbackConfirmState.publishNow)
                }}
                title={t('common.confirmAction')}
                message={rollbackConfirmState?.publishNow
                    ? t('identity.email_templates.rollback_publish_confirm')
                    : t('identity.email_templates.rollback_confirm')}
                variant={rollbackConfirmState?.publishNow ? 'primary' : 'secondary'}
                confirmText={rollbackConfirmState?.publishNow
                    ? t('identity.email_templates.rollback_publish')
                    : t('identity.email_templates.rollback_draft')}
                isLoading={isSaving}
            />

            <Modal
                show={showBodyEditorFullscreenModal}
                onHide={() => setShowBodyEditorFullscreenModal(false)}
                fullscreen
                scrollable>
                <Modal.Header closeButton>
                    <Modal.Title>{t('identity.email_templates.fullscreen_modal_title', 'Template Editor + Live Preview')}</Modal.Title>
                </Modal.Header>
                <Modal.Body>
                    <Row className="g-3">
                        <Col xl={6}>
                            <div className="fw-semibold mb-2">{t('identity.email_templates.fullscreen_editor_title', 'Code Editor')}</div>
                            <div
                                style={{
                                    border: '1px solid #dee2e6',
                                    borderRadius: 8,
                                    overflow: 'hidden',
                                }}>
                                <Editor
                                    height={EMAIL_BODY_EDITOR_FULLSCREEN_HEIGHT}
                                    defaultLanguage="html"
                                    language="html"
                                    theme="vs-dark"
                                    value={form.body}
                                    path={`email-template-fullscreen-${form.id ?? 'draft'}-${form.culture}.html`}
                                    onMount={onBodyEditorMount}
                                    onChange={(value) =>
                                        setForm((prev) => ({ ...prev, body: value ?? '' }))
                                    }
                                    options={{
                                        minimap: { enabled: false },
                                        wordWrap: 'on',
                                        scrollBeyondLastLine: false,
                                        automaticLayout: true,
                                        fontSize: 14,
                                        lineHeight: 22,
                                        tabSize: 2,
                                        insertSpaces: true,
                                        formatOnPaste: true,
                                        formatOnType: true,
                                        padding: { top: 12, bottom: 12 },
                                        renderWhitespace: 'selection',
                                        fontFamily: "'Fira Code', 'Cascadia Code', monospace",
                                        quickSuggestions: {
                                            other: true,
                                            comments: false,
                                            strings: true,
                                        },
                                        suggestOnTriggerCharacters: true,
                                        snippetSuggestions: 'top',
                                        tabCompletion: 'on',
                                    }}
                                />
                            </div>
                            <Form.Text className="text-muted">
                                {t('identity.email_templates.body_intellisense_hint', 'Press Ctrl+Space for IntelliSense suggestions.')}
                            </Form.Text>
                            <div className="small text-muted mt-1">
                                {t('identity.email_templates.fullscreen_shortcuts_hint', 'Shortcuts: Ctrl/Cmd+S save draft, Esc close modal.')}
                            </div>
                        </Col>

                        <Col xl={6}>
                            <div className="fw-semibold mb-2">{t('identity.email_templates.fullscreen_preview_title', 'Live Preview')}</div>
                            <div className="small text-muted mb-2">
                                {t('identity.email_templates.preview_subject')}: <strong>{previewSubject}</strong>
                            </div>
                            <iframe
                                title={t('identity.email_templates.fullscreen_preview_title', 'Live Preview')}
                                srcDoc={previewBody}
                                sandbox="allow-same-origin"
                                style={{
                                    width: '100%',
                                    minHeight: EMAIL_BODY_EDITOR_FULLSCREEN_HEIGHT,
                                    border: '1px solid #dee2e6',
                                    borderRadius: 8,
                                    background: '#fff',
                                }}
                            />
                        </Col>
                    </Row>
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="secondary" onClick={() => setShowBodyEditorFullscreenModal(false)}>
                        {t('common.close')}
                    </Button>
                    <Button
                        variant="primary"
                        disabled={isSaving || isDeleting || isPublishing}
                        onClick={() => void onSave(false)}>
                        {isSaving ? t('common.loading') : t('common.save_draft')}
                    </Button>
                </Modal.Footer>
            </Modal>

            <Modal
                show={showDocumentationModal}
                onHide={() => setShowDocumentationModal(false)}
                size="lg"
                centered
                scrollable>
                <Modal.Header closeButton>
                    <Modal.Title>{t('identity.email_templates.documentation_title')}</Modal.Title>
                </Modal.Header>
                <Modal.Body>
                    <p className="text-muted small mb-3">
                        {t('identity.email_templates.documentation_intro')}
                    </p>

                    <div className="d-flex flex-column gap-3">
                        {documentationSections.map((section) => (
                            <div key={section.title}>
                                <h6 className="mb-2">{section.title}</h6>
                                <ul className="mb-0 ps-3">
                                    {section.items.map((item) => (
                                        <li key={`${section.title}-${item.label}`} className="mb-2">
                                            <div className="fw-semibold">{item.label}</div>
                                            <div className="text-muted small">{item.description}</div>
                                        </li>
                                    ))}
                                </ul>
                            </div>
                        ))}
                    </div>
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="secondary" onClick={() => setShowDocumentationModal(false)}>
                        {t('identity.email_templates.documentation_close', 'Close')}
                    </Button>
                </Modal.Footer>
            </Modal>
        </VerticalLayout>
    )
}

export default EmailTemplates
