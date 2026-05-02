import { useCallback, useEffect, useState, memo, useMemo } from 'react'
import { Button, Card, Col, Container, Form, Row, Table, CardHeader } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { LuPlus, LuTrash2, LuShieldCheck, LuMapPin, LuCalendar, LuPrinter, LuInfo, LuSave } from 'react-icons/lu'
import { showToast } from '@/utils/toast'
import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import PropertyManagementService from '@/services/propertyManagementService'
import type { SiteListItem, GeneralAssemblyListItem, ResidentListItem, GeneralAssemblyBoardMember } from '@/types/propertyManagement'
import { BoardType, BoardMemberType } from '@/types/propertyManagement'
import { TbGavel } from 'react-icons/tb'

const AuditBoardPage = () => {
    const { t } = useTranslation()
    const [sites, setSites] = useState<SiteListItem[]>([])
    const [assemblies, setAssemblies] = useState<GeneralAssemblyListItem[]>([])
    const [residents, setResidents] = useState<ResidentListItem[]>([])
    
    const [selectedSiteId, setSelectedSiteId] = useState('')
    const [selectedAssemblyId, setSelectedAssemblyId] = useState('')
    const [assemblyDetail, setAssemblyDetail] = useState<any>(null)
    const [loading, setLoading] = useState(false)
    const [isSaving, setIsSaving] = useState(false)

    useEffect(() => {
        const fetchSites = async () => {
            const result = await PropertyManagementService.getSites({ pageNumber: 1, pageSize: 100 })
            setSites(result.items)
        }
        fetchSites()
    }, [])

    useEffect(() => {
        if (selectedSiteId) {
            const fetchAssemblies = async () => {
                const result = await PropertyManagementService.getGeneralAssemblies({ siteId: selectedSiteId, pageNumber: 1, pageSize: 100 })
                setAssemblies(result.items)
            }
            const fetchResidents = async () => {
                const result = await PropertyManagementService.getResidents({ siteId: selectedSiteId, pageNumber: 1, pageSize: 100 })
                setResidents(result.items)
            }
            fetchAssemblies()
            fetchResidents()
        } else {
            setAssemblies([])
            setResidents([])
        }
    }, [selectedSiteId])

    const fetchDetail = useCallback(async () => {
        if (!selectedAssemblyId) return
        setLoading(true)
        try {
            const data = await PropertyManagementService.getGeneralAssembly(selectedAssemblyId)
            setAssemblyDetail(data)
        } catch (error) {
            showToast(t('common.error'), 'danger')
        } finally {
            setLoading(false)
        }
    }, [selectedAssemblyId, t])

    useEffect(() => {
        fetchDetail()
    }, [fetchDetail])

    const addMember = () => {
        if (!assemblyDetail) return
        const newMember: GeneralAssemblyBoardMember = {
            residentId: '',
            boardType: BoardType.AuditBoard,
            memberType: BoardMemberType.Principal,
            title: '',
            isActive: true
        }
        setAssemblyDetail({ ...assemblyDetail, boardMembers: [...assemblyDetail.boardMembers, newMember] })
    }

    const updateMember = (idx: number, field: keyof GeneralAssemblyBoardMember, value: any) => {
        setAssemblyDetail((prev: any) => {
            if (!prev) return prev;
            const newMembers = [...prev.boardMembers]
            newMembers[idx] = { ...newMembers[idx], [field]: value }
            return { ...prev, boardMembers: newMembers }
        })
    }

    const removeMember = (idx: number) => {
        const newMembers = assemblyDetail.boardMembers.filter((_: any, i: number) => i !== idx)
        setAssemblyDetail({ ...assemblyDetail, boardMembers: newMembers })
    }

    const save = async () => {
        if (isSaving) return;

        // Validation: Ensure all members have a resident selected
        const hasEmptyResident = assemblyDetail.boardMembers.some((m: any) => !m.residentId);
        if (hasEmptyResident) {
            showToast("Lütfen tüm üyeler için bir kişi seçiniz.", 'warning');
            return;
        }

        setIsSaving(true);
        try {
            await PropertyManagementService.updateGeneralAssembly(selectedAssemblyId, assemblyDetail)
            showToast(t('common.success'), 'success')
        } catch (error: any) {
            console.error('Save error:', error);
            showToast(error.message || t('common.error'), 'danger')
        } finally {
            setIsSaving(false);
        }
    }

    const printBoard = () => {
        window.print()
    }

    const assignedResidentIds = useMemo(() => {
        return assemblyDetail?.boardMembers?.map((m: any) => m.residentId).filter(Boolean) || []
    }, [assemblyDetail?.boardMembers])

    return (
        <VerticalLayout>
            <PageBreadcrumb title="Genel Kurul İşlemleri" subtitle="Denetim Kurulu Belirleme" />
            <Container fluid>
                <Card className="border-0 shadow-sm overflow-hidden mb-4">
                    <CardHeader className="border-bottom border-light p-3 print-hide">
                        <div className="d-flex flex-wrap align-items-center justify-content-between gap-3 w-100">
                            <div className="d-flex flex-wrap gap-2 align-items-center">
                                {/* Site Filter */}
                                <div className="app-search" style={{ minWidth: '200px' }}>
                                    <Form.Select 
                                        value={selectedSiteId} 
                                        onChange={(e) => setSelectedSiteId(e.target.value)}
                                    >
                                        <option value="">Site Seçiniz...</option>
                                        {sites.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                                    </Form.Select>
                                    <LuMapPin className="app-search-icon text-muted" />
                                </div>

                                {/* Assembly Filter */}
                                <div className="app-search" style={{ minWidth: '240px' }}>
                                    <Form.Select 
                                        value={selectedAssemblyId} 
                                        onChange={(e) => setSelectedAssemblyId(e.target.value)}
                                        disabled={!selectedSiteId}
                                    >
                                        <option value="">Genel Kurul Seçiniz...</option>
                                        {assemblies.map(a => <option key={a.id} value={a.id}>{a.term} - {new Date(a.meetingDate).toLocaleDateString('tr-TR')}</option>)}
                                    </Form.Select>
                                    <LuCalendar className="app-search-icon text-muted" />
                                </div>

                                <Button 
                                    variant="outline-primary" 
                                    disabled={!selectedAssemblyId}
                                    onClick={fetchDetail}
                                >
                                    Listele
                                </Button>
                            </div>

                            <div className="d-flex gap-2">
                                {assemblyDetail && (
                                    <>
                                        <Button variant="soft-secondary" size="sm" onClick={printBoard} className="btn-icon">
                                            <LuPrinter size={18} />
                                        </Button>
                                        <Button variant="soft-warning" size="sm" onClick={addMember} className="d-flex align-items-center gap-1 text-warning">
                                            <LuPlus /> Üye Ekle
                                        </Button>
                                        <Button 
                                            variant="warning" 
                                            size="sm" 
                                            onClick={save} 
                                            className="px-3 shadow-sm text-white d-flex align-items-center gap-2"
                                            disabled={isSaving}
                                        >
                                            {isSaving ? <span className="spinner-border spinner-border-sm" /> : <LuSave />}
                                            Kaydet
                                        </Button>
                                    </>
                                )}
                            </div>
                        </div>
                    </CardHeader>

                    {loading ? (
                        <div className="text-center py-5">
                            <div className="spinner-border text-primary" role="status">
                                <span className="visually-hidden">Yükleniyor...</span>
                            </div>
                            <p className="mt-2 text-muted small">Veriler yükleniyor...</p>
                        </div>
                    ) : assemblyDetail ? (
                        <Card.Body className="p-0">
                            <div className="table-responsive">
                                <Table hover className="table-custom table-centered mb-0 align-middle">
                                    <thead className="bg-light align-middle bg-opacity-25 thead-sm">
                                        <tr className="text-uppercase fs-xxs">
                                            <th className="ps-4 py-2" style={{ width: '40%' }}>Ad Soyad / Sakin</th>
                                            <th style={{ width: '20%' }}>Üyelik Tipi</th>
                                            <th style={{ width: '30%' }}>Görev / Ünvan</th>
                                            <th className="text-end pe-4 print-hide">İşlem</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {assemblyDetail.boardMembers.filter((m: any) => m.boardType === BoardType.AuditBoard).length === 0 ? (
                                            <tr>
                                                <td colSpan={4} className="text-center py-5">
                                                    <div className="py-4">
                                                        <LuShieldCheck size={48} className="text-muted mb-3 opacity-25" />
                                                        <h5 className="text-muted">Henüz denetim kurulu üyesi eklenmemiş.</h5>
                                                    </div>
                                                </td>
                                            </tr>
                                        ) : (
                                            <>
                                                {/* Principal Members Section */}
                                                <tr className="bg-soft-warning bg-opacity-10 border-top">
                                                    <td colSpan={4} className="ps-4 py-2 fw-bold text-warning small text-uppercase" style={{ fontSize: '0.65rem' }}>
                                                        Asil Üyeler
                                                    </td>
                                                </tr>
                                                {assemblyDetail.boardMembers
                                                    .map((m: any, idx: number) => ({ ...m, originalIdx: idx }))
                                                    .filter((m: any) => m.boardType === BoardType.AuditBoard && m.memberType === BoardMemberType.Principal)
                                                    .map((member: any) => (
                                                        <MemberRow 
                                                            key={member.originalIdx}
                                                            member={member} 
                                                            idx={member.originalIdx} 
                                                            residents={residents} 
                                                            assignedResidentIds={assignedResidentIds}
                                                            updateMember={updateMember} 
                                                            removeMember={removeMember} 
                                                        />
                                                    ))}

                                                {/* Substitute Members Section */}
                                                <tr className="bg-soft-secondary bg-opacity-10 border-top">
                                                    <td colSpan={4} className="ps-4 py-2 fw-bold text-secondary small text-uppercase" style={{ fontSize: '0.65rem' }}>
                                                        Yedek Üyeler
                                                    </td>
                                                </tr>
                                                {assemblyDetail.boardMembers
                                                    .map((m: any, idx: number) => ({ ...m, originalIdx: idx }))
                                                    .filter((m: any) => m.boardType === BoardType.AuditBoard && m.memberType === BoardMemberType.Substitute)
                                                    .map((member: any) => (
                                                        <MemberRow 
                                                            key={member.originalIdx}
                                                            member={member} 
                                                            idx={member.originalIdx} 
                                                            residents={residents} 
                                                            assignedResidentIds={assignedResidentIds}
                                                            updateMember={updateMember} 
                                                            removeMember={removeMember} 
                                                        />
                                                    ))}
                                            </>
                                        )}
                                    </tbody>
                                </Table>
                            </div>
                        </Card.Body>
                    ) : (
                        <div className="text-center py-5">
                            <TbGavel size={64} className="text-primary mb-3 opacity-25" />
                            <h4 className="text-dark fw-bold">Denetim Kurulu Yönetimi</h4>
                            <p className="text-muted mx-auto" style={{ maxWidth: '400px' }}>
                                Denetim kurulu üyelerini görüntülemek ve düzenlemek için lütfen yukarıdan bir site ve genel kurul dönemi seçiniz.
                            </p>
                        </div>
                    )}
                    
                    {assemblyDetail && (
                        <Card.Footer className="bg-white border-top-0 p-3 text-center text-muted small">
                            <LuInfo className="me-1" /> {assemblyDetail.siteName} - {assemblyDetail.term} Dönemi Denetim Kurulu Listesi
                        </Card.Footer>
                    )}
                </Card>
            </Container>

            <style>{`
                .app-search { position: relative; }
                .app-search .form-select, .app-search .form-control { padding-left: 2.5rem !important; border-radius: 4px; }
                .app-search-icon { position: absolute; left: 12px; top: 50%; transform: translateY(-50%); font-size: 16px; z-index: 1; }
                
                @media print {
                    .print-hide { display: none !important; }
                    .card { border: none !important; box-shadow: none !important; }
                    body { background: white !important; }
                    .container-fluid { padding: 0 !important; }
                }
                .table-centered td { vertical-align: middle; }
                .member-select {
                    border: 1px solid transparent;
                    transition: all 0.2s;
                }
                .member-select:focus {
                    border-color: var(--bs-warning);
                    background-color: white;
                }
                .table-custom tr:hover { background-color: rgba(var(--bs-primary-rgb), 0.05); }
                .thead-sm th { padding-top: 0.5rem !important; padding-bottom: 0.5rem !important; font-weight: 600; color: var(--bs-secondary-color); }
                .fs-xxs { font-size: 0.7rem !important; letter-spacing: 0.02em; }
                .btn-icon { width: 32px; height: 32px; padding: 0; display: flex; align-items: center; justify-content: center; }
            `}</style>
        </VerticalLayout>
    )
}

const MemberRow = memo(({ member, idx, residents, assignedResidentIds, updateMember, removeMember }: any) => {
    return (
        <tr>
            <td className="ps-4">
                <div className="d-flex align-items-center gap-3">
                    <div className="avatar-sm bg-soft-warning text-warning rounded-circle d-flex align-items-center justify-content-center fw-bold">
                        {member.residentId ? residents.find((r: any) => r.id === member.residentId)?.firstName?.charAt(0) : '?'}
                    </div>
                    <Form.Select 
                        size="sm"
                        className="member-select bg-light border-0"
                        value={member.residentId}
                        onChange={(e) => updateMember(idx, 'residentId', e.target.value)}
                    >
                        <option value="">Kişi Seçiniz...</option>
                        {residents.map((r: any) => (
                            <option 
                                key={r.id} 
                                value={r.id}
                                disabled={(assignedResidentIds || []).includes(r.id) && r.id !== member.residentId}
                            >
                                {r.firstName} {r.lastName} {r.unitNumber ? `(D:${r.unitNumber})` : ''}
                                {(assignedResidentIds || []).includes(r.id) && r.id !== member.residentId ? ' (Seçili)' : ''}
                            </option>
                        ))}
                    </Form.Select>
                </div>
            </td>
            <td>
                <Form.Select 
                    size="sm"
                    className="bg-light border-0"
                    value={member.memberType}
                    onChange={(e) => {
                        const newType = Number(e.target.value);
                        updateMember(idx, 'memberType', newType);
                        if (newType === BoardMemberType.Substitute) {
                            updateMember(idx, 'title', 'Yedek Denetçi');
                        }
                    }}
                >
                    <option value={BoardMemberType.Principal}>Asil Üye</option>
                    <option value={BoardMemberType.Substitute}>Yedek Üye</option>
                </Form.Select>
            </td>
            <td>
                <Form.Select 
                    size="sm"
                    className="bg-light border-0"
                    value={member.title || ''} 
                    onChange={(e) => updateMember(idx, 'title', e.target.value)}
                    disabled={member.memberType === BoardMemberType.Substitute}
                >
                    {member.memberType === BoardMemberType.Substitute ? (
                        <option value="Yedek Denetçi">Yedek Denetçi</option>
                    ) : (
                        <>
                            <option value="">Seçiniz...</option>
                            <option value="Denetçi">Denetçi</option>
                            <option value="Üye">Üye</option>
                        </>
                    )}
                </Form.Select>
            </td>
            <td className="text-end pe-4 print-hide">
                <Button variant="link" className="text-danger p-0" onClick={() => removeMember(idx)}>
                    <LuTrash2 />
                </Button>
            </td>
        </tr>
    )
})

export default AuditBoardPage
