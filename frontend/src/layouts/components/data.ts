import { type MenuItemType } from '@/types/layout'
import { type IconType } from 'react-icons'
import {
    TbLogout2,
    TbUserCircle,
    TbWorld,
    TbMail,
    TbHistory,
    TbAdjustmentsHorizontal,
    TbSend,
    TbUsers,
    TbFileDescription,
    TbHome,
    TbBuildingCommunity,
    TbBuilding,
    TbLayoutGrid,
} from 'react-icons/tb'
import {
    LuCircleGauge,
} from 'react-icons/lu'

type UserDropdownItemType = {
    label?: string
    labelKey?: string
    icon?: IconType
    url?: string
    isDivider?: boolean
    isHeader?: boolean
    class?: string
}

export const userDropdownItems: UserDropdownItemType[] = [
    {
        label: '',
        labelKey: 'topbar.welcome',
        isHeader: true,
    },
    {
        label: '',
        labelKey: 'topbar.profile',
        icon: TbUserCircle,
        url: '/users/profile',
    },
    {
        label: '',
        labelKey: 'topbar.logout',
        icon: TbLogout2,
        class: 'text-danger fw-semibold',
    },
]

export const menuItems: MenuItemType[] = [
    { key: 'navigation', label: '', labelKey: 'menu.navigation', isTitle: true },
    {
        key: 'dashboards',
        label: '',
        labelKey: 'menu.dashboard',
        icon: LuCircleGauge,
        url: '/dashboard',
    },
    { key: 'property-mgmt-section', label: '', labelKey: 'propertyManagement.title', isTitle: true },
    { key: 'sites', label: '', labelKey: 'propertyManagement.tabs.sites', url: '/management/properties/sites', permission: 'Sites.Read', icon: TbBuildingCommunity },
    { key: 'blocks', label: '', labelKey: 'propertyManagement.tabs.blocks', url: '/management/properties/blocks', permission: 'Blocks.Read', icon: TbBuilding },
    { key: 'units', label: '', labelKey: 'propertyManagement.tabs.units', url: '/management/properties/units', permission: 'Units.Read', icon: TbLayoutGrid },
    { key: 'residents', label: '', labelKey: 'propertyManagement.tabs.residents', url: '/management/properties/residents', permission: 'Residents.Read', icon: TbUsers },
    { key: 'apps', label: '', labelKey: 'menu.apps', isTitle: true },
    {
        key: 'localization-mgmt',
        label: '',
        labelKey: 'identity.localization.title',
        icon: TbWorld,
        url: '/admin/localization',
        permission: 'Localization.Manage'
    },
    {
        key: 'system-settings',
        label: '',
        labelKey: 'identity.system_settings.title',
        icon: TbAdjustmentsHorizontal,
        url: '/admin/settings',
        permission: 'Settings.Manage',
    },
    {
        key: 'email-templates',
        label: '',
        labelKey: 'identity.email_templates.title',
        icon: TbMail,
        url: '/admin/email-templates',
        permission: 'Email.Manage',
    },
    {
        key: 'audit-logs',
        label: '',
        labelKey: 'identity.audit_logs.title',
        icon: TbHistory,
        url: '/admin/audit-logs',
        permission: 'Audit.Read',
    },
    {
        key: 'email-delivery-logs',
        label: '',
        labelKey: 'identity.email_delivery_logs.title',
        icon: TbSend,
        url: '/admin/email-delivery-logs',
        permission: 'Email.Read',
    },
    {
        key: 'compliance-terms',
        label: '',
        labelKey: 'identity.compliance_terms.title',
        icon: TbFileDescription,
        url: '/admin/compliance-terms',
        permission: 'Compliance.Read',
    },
    {
        key: 'identity',
        label: '',
        labelKey: 'identity.title',
        icon: TbUsers,
        children: [
            { key: 'user-mgmt', label: '', labelKey: 'identity.users', url: '/admin/users', permission: 'Users.Read' },
            { key: 'role-mgmt', label: '', labelKey: 'identity.roles', url: '/admin/roles', permission: 'Roles.Read' },
            { key: 'perm-mgmt', label: '', labelKey: 'identity.permissions', url: '/admin/permissions', permission: 'Roles.Manage' },
        ],
    },
]

export const horizontalMenuItems: MenuItemType[] = [
    {
        key: 'dashboards',
        label: '',
        labelKey: 'menu.dashboard',
        icon: LuCircleGauge,
        url: '/dashboard',
    },
]
