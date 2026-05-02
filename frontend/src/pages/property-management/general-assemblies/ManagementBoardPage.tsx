import { useCallback, useEffect, useState, memo, useMemo } from 'react'
import { Badge, Button, Card, CardHeader, Container, Form, Modal, Table } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { LuCalendar, LuInfo, LuMapPin, LuPlus, LuPrinter, LuSave, LuTrash2, LuUserCheck, LuUserPlus } from 'react-icons/lu'
import { showToast } from '@/utils/toast'
import VerticalLayout from '@/layouts/VerticalLayout'
import PageBreadcrumb from '@/components/PageBreadcrumb'
import PropertyManagementService from '@/services/propertyManagementService'
import type { SiteListItem, GeneralAssemblyListItem, ResidentListItem, GeneralAssemblyBoardMember } from '@/types/propertyManagement'
import { BoardType, BoardMemberType } from '@/types/propertyManagement'
import { TbGavel } from 'react-icons/tb'

const ManagementBoardPage = () => {
    const { t } = useTranslation()
    const [sites, setSites] = useState<SiteListItem[]>([])
    const [assemblies, setAssemblies] = useState<GeneralAssemblyListItem[]>([])
    const [residents, setResidents] = useState<ResidentListItem[]>([])

    const [selectedSiteId, setSelectedSiteId] = useState('')
    const [selectedAssemblyId, setSelectedAssemblyId] = useState('')
    const [assemblyDetail, setAssemblyDetail] = useState<any>(null)
    const [loading, setLoading] = useState(false)
    const [isSaving, setIsSaving] = useState(false)
    const [showConfirm, setShowConfirm] = useState(false)
    const [showSubstitutePromotion, setShowSubstitutePromotion] = useState(false)
    const [resigningPrincipalId, setResigningPrincipalId] = useState('')
    const [promotingSubstituteId, setPromotingSubstituteId] = useState('')

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
        if (!selectedAssemblyId) {
            setAssemblyDetail(null)
            return
        }

        setLoading(true)
        try {
            const data = await PropertyManagementService.getGeneralAssembly(selectedAssemblyId)
            setAssemblyDetail(data)
        } catch (error) {
            showToast(t('common.error'), 'error')
        } finally {
            setLoading(false)
        }
    }, [selectedAssemblyId, t])

    useEffect(() => {
        fetchDetail()
    }, [fetchDetail])

    const mBoardMembers = useMemo(() => {
        return assemblyDetail?.boardMembers?.filter((m: any) => m.boardType === BoardType.ManagementBoard) || []
    }, [assemblyDetail?.boardMembers])

    const isManagementBoardLocked = useMemo(() => {
        return mBoardMembers.some((member: any) => Boolean(member.id))
    }, [mBoardMembers])

    const principalMembers = useMemo(() => {
        return mBoardMembers.filter((member: any) => member.memberType === BoardMemberType.Principal && member.isActive !== false)
    }, [mBoardMembers])

    const substituteMembers = useMemo(() => {
        return mBoardMembers.filter((member: any) => member.memberType === BoardMemberType.Substitute && member.isActive !== false)
    }, [mBoardMembers])

    const addMember = () => {
        if (!assemblyDetail) return
        if (isManagementBoardLocked) {
            showToast('Onaylanmış yönetim kurulu üzerinde değişiklik yapılamaz.', 'warning')
            return
        }

        const newMember: GeneralAssemblyBoardMember = {
            residentId: '',
            boardType: BoardType.ManagementBoard,
            memberType: BoardMemberType.Principal,
            title: '',
            isActive: true
        }
        setAssemblyDetail({ ...assemblyDetail, boardMembers: [...assemblyDetail.boardMembers, newMember] })
    }

    const updateMember = (idx: number, field: keyof GeneralAssemblyBoardMember, value: any) => {
        if (isManagementBoardLocked) {
            showToast('Onaylanmış yönetim kurulu üzerinde değişiklik yapılamaz.', 'warning')
            return
        }

        setAssemblyDetail((prev: any) => {
            if (!prev) return prev;
            const newMembers = [...prev.boardMembers]
            newMembers[idx] = { ...newMembers[idx], [field]: value }
            return { ...prev, boardMembers: newMembers }
        })
    }

    const removeMember = (idx: number) => {
        if (!assemblyDetail) return
        if (isManagementBoardLocked) {
            showToast('Onaylanmış yönetim kurulu üzerinde değişiklik yapılamaz.', 'warning')
            return
        }

        const newMembers = assemblyDetail.boardMembers.filter((_: any, i: number) => i !== idx)
        setAssemblyDetail({ ...assemblyDetail, boardMembers: newMembers })
    }

    const openSubstitutePromotion = () => {
        if (!assemblyDetail || !selectedAssemblyId) return
        if (!isManagementBoardLocked) return

        if (principalMembers.length === 0) {
            showToast('İstifa edecek asil üye bulunmuyor.', 'warning')
            return
        }

        if (substituteMembers.length === 0) {
            showToast('Asil üyeliğe geçirilecek yedek üye bulunmuyor.', 'warning')
            return
        }

        setResigningPrincipalId(principalMembers[0].residentId)
        setPromotingSubstituteId(substituteMembers[0].residentId)
        setShowSubstitutePromotion(true)
    }

    const closeSubstitutePromotion = () => {
        setShowSubstitutePromotion(false)
        setResigningPrincipalId('')
        setPromotingSubstituteId('')
    }

    const openSaveConfirm = () => {
        if (isSaving) return
        if (!assemblyDetail || !selectedAssemblyId) return
        if (isManagementBoardLocked) {
            showToast('Onaylanmış yönetim kurulu üzerinde değişiklik yapılamaz.', 'warning')
            return
        }

        const hasEmptyResident = assemblyDetail.boardMembers.some((m: any) => !m.residentId)
        if (hasEmptyResident) {
            showToast('Lütfen tüm üyeler için bir kişi seçiniz.', 'warning')
            return
        }

        setShowConfirm(true)
    }

    const getPromotedTitle = (title?: string | null) => {
        const normalizedTitle = title?.trim()
        return normalizedTitle && normalizedTitle !== 'Yedek Üye' ? normalizedTitle : 'Üye'
    }

    const confirmSubstitutePromotion = async () => {
        if (isSaving || !assemblyDetail || !selectedAssemblyId) return

        const resigningMember = principalMembers.find((member: any) => member.residentId === resigningPrincipalId)
        const substituteMember = substituteMembers.find((member: any) => member.residentId === promotingSubstituteId)

        if (!resigningMember || !substituteMember) {
            showToast('İstifa edecek asil üye ve yerine geçecek yedek üye seçilmelidir.', 'warning')
            return
        }

        const promotedTitle = getPromotedTitle(resigningMember.title)
        const updatedBoardMembers = assemblyDetail.boardMembers
            .map((member: any) => {
                if (member.boardType === BoardType.ManagementBoard && member.residentId === resigningPrincipalId) {
                    return {
                        ...member,
                        isActive: false
                    }
                }
                if (member.boardType === BoardType.ManagementBoard && member.residentId === promotingSubstituteId) {
                    return {
                        ...member,
                        memberType: BoardMemberType.Principal,
                        title: promotedTitle,
                        isActive: true
                    }
                }

                return member
            })

        setIsSaving(true)
        try {
            const updatedAssembly = await PropertyManagementService.updateGeneralAssembly(selectedAssemblyId, {
                ...assemblyDetail,
                boardMembers: updatedBoardMembers
            })

            if (updatedAssembly) {
                setAssemblyDetail(updatedAssembly)
            }

            closeSubstitutePromotion()
            showToast('Yedek üye asil üyeliğe geçirildi.', 'success')
        } catch (error: any) {
            console.error('Substitute promotion error:', error)
            if (error?.response?.status !== 400) {
                showToast(error?.response?.data?.detail || error?.response?.data?.message || error.message || t('common.error'), 'error')
            }
        } finally {
            setIsSaving(false)
        }
    }

    const save = async () => {
        if (isSaving || !assemblyDetail || !selectedAssemblyId) return

        setIsSaving(true)
        try {
            const updatedAssembly = await PropertyManagementService.updateGeneralAssembly(selectedAssemblyId, assemblyDetail)
            if (updatedAssembly) {
                setAssemblyDetail(updatedAssembly)
            }
            setShowConfirm(false)
            showToast(t('common.success'), 'success')
        } catch (error: any) {
            console.error('Save error:', error)
            if (error?.response?.status !== 400) {
                showToast(error?.response?.data?.detail || error?.response?.data?.message || error.message || t('common.error'), 'error')
            }
        } finally {
            setIsSaving(false)
        }
    }

    const confirmationMembers = useMemo(() => {
        return [...mBoardMembers].sort((a: any, b: any) => a.memberType - b.memberType)
    }, [mBoardMembers])

    const selectedResigningPrincipal = useMemo(() => {
        return principalMembers.find((member: any) => member.residentId === resigningPrincipalId)
    }, [principalMembers, resigningPrincipalId])

    const selectedPromotingSubstitute = useMemo(() => {
        return substituteMembers.find((member: any) => member.residentId === promotingSubstituteId)
    }, [substituteMembers, promotingSubstituteId])

    const printBoard = () => {
        window.print()
    }

    const assignedResidentIds = useMemo(() => {
        return assemblyDetail?.boardMembers?.map((m: any) => m.residentId).filter(Boolean) || []
    }, [assemblyDetail?.boardMembers])

    const getResidentDisplay = (member: any) => {
        const resident = residents.find((r: any) => r.id === member.residentId)
        if (resident) {
            return `${resident.firstName} ${resident.lastName}${resident.unitNumber ? ` (D:${resident.unitNumber})` : ''}`
        }

        if (member.residentName) {
            return `${member.residentName}${member.unitNumber ? ` (D:${member.unitNumber})` : ''}`
        }

        return 'Seçilmedi'
    }

    const getMemberTypeLabel = (memberType: BoardMemberType) => {
        return memberType === BoardMemberType.Substitute ? 'Yedek Üye' : 'Asil Üye'
    }

    return (
        <VerticalLayout>
            <PageBreadcrumb title="Genel Kurul İşlemleri" subtitle="Yönetim Kurulu Belirleme" />
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

                            <div className="d-flex gap-2 align-items-center">
                                {assemblyDetail && (
                                    <>
                                        <Button variant="soft-secondary" size="sm" onClick={printBoard} className="btn-icon">
                                            <LuPrinter size={18} />
                                        </Button>
                                        {isManagementBoardLocked ? (
                                            <>
                                                <Button
                                                    variant="soft-warning"
                                                    size="sm"
                                                    onClick={openSubstitutePromotion}
                                                    className="d-flex align-items-center gap-1"
                                                    disabled={isSaving}
                                                >
                                                    <LuUserCheck /> Yedekten Ata
                                                </Button>
                                                <Badge bg="secondary" className="py-2 px-3">
                                                    Onaylandı
                                                </Badge>
                                            </>
                                        ) : (
                                            <>
                                                <Button variant="soft-success" size="sm" onClick={addMember} className="d-flex align-items-center gap-1">
                                                    <LuPlus /> Üye Ekle
                                                </Button>
                                                <Button
                                                    variant="success"
                                                    size="sm"
                                                    onClick={openSaveConfirm}
                                                    className="px-3 shadow-sm text-white d-flex align-items-center gap-2"
                                                    disabled={isSaving}
                                                >
                                                    {isSaving ? <span className="spinner-border spinner-border-sm" /> : <LuSave />}
                                                    Kaydet
                                                </Button>
                                            </>
                                        )}
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
                            {isManagementBoardLocked && (
                                <div className="print-hide border-bottom bg-light px-4 py-3 small text-muted d-flex align-items-center gap-2">
                                    <LuInfo className="text-primary" />
                                    Yönetim kurulu onaylandı. Normal düzenleme kapalıdır; yalnızca asil üye istifasında yedekten atama yapılabilir.
                                </div>
                            )}

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
                                        {mBoardMembers.length === 0 ? (
                                            <tr>
                                                <td colSpan={4} className="text-center py-5">
                                                    <div className="py-4">
                                                        <LuUserPlus size={48} className="text-muted mb-3 opacity-25" />
                                                        <h5 className="text-muted">Henüz yönetim kurulu üyesi eklenmemiş.</h5>
                                                    </div>
                                                </td>
                                            </tr>
                                        ) : (
                                            <>
                                                {/* Principal Members Section */}
                                                <tr className="bg-soft-success bg-opacity-10 border-top">
                                                    <td colSpan={4} className="ps-4 py-2 fw-bold text-success small text-uppercase" style={{ fontSize: '0.65rem' }}>
                                                        Asil Üyeler
                                                    </td>
                                                </tr>
                                                {assemblyDetail.boardMembers
                                                    .map((m: any, idx: number) => ({ ...m, originalIdx: idx }))
                                                    .filter((m: any) => m.boardType === BoardType.ManagementBoard && m.memberType === BoardMemberType.Principal)
                                                    .map((member: any) => (
                                                        <MemberRow
                                                            key={member.originalIdx}
                                                            member={member}
                                                            idx={member.originalIdx}
                                                            residents={residents}
                                                            assignedResidentIds={assignedResidentIds}
                                                            updateMember={updateMember}
                                                            removeMember={removeMember}
                                                            disabled={isManagementBoardLocked}
                                                        />
                                                    ))}

                                                {/* Substitute Members Section */}
                                                <tr className="bg-soft-warning bg-opacity-10 border-top">
                                                    <td colSpan={4} className="ps-4 py-2 fw-bold text-warning small text-uppercase" style={{ fontSize: '0.65rem' }}>
                                                        Yedek Üyeler
                                                    </td>
                                                </tr>
                                                {assemblyDetail.boardMembers
                                                    .map((m: any, idx: number) => ({ ...m, originalIdx: idx }))
                                                    .filter((m: any) => m.boardType === BoardType.ManagementBoard && m.memberType === BoardMemberType.Substitute)
                                                    .map((member: any) => (
                                                        <MemberRow
                                                            key={member.originalIdx}
                                                            member={member}
                                                            idx={member.originalIdx}
                                                            residents={residents}
                                                            assignedResidentIds={assignedResidentIds}
                                                            updateMember={updateMember}
                                                            removeMember={removeMember}
                                                            disabled={isManagementBoardLocked}
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
                            <h4 className="text-dark fw-bold">Yönetim Kurulu Yönetimi</h4>
                            <p className="text-muted mx-auto" style={{ maxWidth: '400px' }}>
                                Yönetim kurulu üyelerini görüntülemek ve düzenlemek için lütfen yukarıdan bir site ve genel kurul dönemi seçiniz.
                            </p>
                        </div>
                    )}

                    {assemblyDetail && (
                        <Card.Footer className="bg-white border-top-0 p-3 text-center text-muted small">
                            <LuInfo className="me-1" /> {assemblyDetail.siteName} - {assemblyDetail.term} Dönemi Yönetim Kurulu Listesi
                            {isManagementBoardLocked && ' - Sadece asil istifasında yedekten atama yapılabilir.'}
                        </Card.Footer>
                    )}
                </Card>

                <Modal
                    show={showConfirm}
                    onHide={() => !isSaving && setShowConfirm(false)}
                    size="lg"
                    centered
                    backdrop={isSaving ? 'static' : true}
                    keyboard={!isSaving}
                >
                    <Modal.Header closeButton={!isSaving}>
                        <Modal.Title>Yönetim Kurulu Onayı</Modal.Title>
                    </Modal.Header>
                    <Modal.Body>
                        <div className="mb-3">
                            <div className="fw-semibold text-dark mb-1">
                                {assemblyDetail?.siteName} - {assemblyDetail?.term} Dönemi
                            </div>
                            <div className="text-muted small">
                                Aşağıdaki yönetim kurulu seçimini bu şekilde kaydetmek üzeresiniz.
                            </div>
                        </div>

                        <div className="border rounded p-3 bg-warning bg-opacity-10 text-warning-emphasis mb-3">
                            Onayladıktan sonra geri alınamaz.
                        </div>

                        <div className="table-responsive border rounded">
                            <Table className="mb-0 align-middle">
                                <thead className="bg-light">
                                    <tr>
                                        <th className="ps-3">Kişi</th>
                                        <th>Üyelik Tipi</th>
                                        <th>Görev / Ünvan</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {confirmationMembers.length === 0 ? (
                                        <tr>
                                            <td colSpan={3} className="text-center text-muted py-4">
                                                Kaydedilecek yönetim kurulu üyesi bulunmuyor.
                                            </td>
                                        </tr>
                                    ) : confirmationMembers.map((member: any, idx: number) => (
                                        <tr key={`${member.residentId}-${idx}`}>
                                            <td className="ps-3 fw-medium">{getResidentDisplay(member)}</td>
                                            <td>
                                                <Badge bg={member.isActive === false ? 'secondary' : (member.memberType === BoardMemberType.Principal ? 'success' : 'warning')}>
                                                    {member.isActive === false ? 'Pasif' : getMemberTypeLabel(member.memberType)}
                                                </Badge>
                                            </td>
                                            <td className={member.isActive === false ? 'text-decoration-line-through text-muted' : ''}>
                                                {member.title || '-'}
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </Table>
                        </div>
                    </Modal.Body>
                    <Modal.Footer>
                        <Button variant="light" onClick={() => setShowConfirm(false)} disabled={isSaving}>
                            Düzenlemeye Dön
                        </Button>
                        <Button variant="success" onClick={save} disabled={isSaving}>
                            {isSaving ? (
                                <>
                                    <span className="spinner-border spinner-border-sm me-1" />
                                    Kaydediliyor...
                                </>
                            ) : (
                                'Onayla ve Kaydet'
                            )}
                        </Button>
                    </Modal.Footer>
                </Modal>

                <Modal
                    show={showSubstitutePromotion}
                    onHide={() => !isSaving && closeSubstitutePromotion()}
                    size="lg"
                    centered
                    backdrop={isSaving ? 'static' : true}
                    keyboard={!isSaving}
                >
                    <Modal.Header closeButton={!isSaving}>
                        <Modal.Title>Asil Üye İstifa / Yedekten Atama</Modal.Title>
                    </Modal.Header>
                    <Modal.Body>
                        <div className="mb-3">
                            <div className="fw-semibold text-dark mb-1">
                                {assemblyDetail?.siteName} - {assemblyDetail?.term} Dönemi
                            </div>
                            <div className="text-muted small">
                                Bu işlem yalnızca asil üye istifa ettiğinde kullanılabilir.
                            </div>
                        </div>

                        <div className="border rounded p-3 bg-warning bg-opacity-10 text-warning-emphasis mb-3">
                            Seçilen asil üye yönetim kurulu listesinden çıkarılır, seçilen yedek üye asil üye olarak kaydedilir. Onayladıktan sonra geri alınamaz.
                        </div>

                        <div className="row g-3">
                            <div className="col-md-6">
                                <Form.Group>
                                    <Form.Label className="fw-semibold">İstifa Eden Asil Üye</Form.Label>
                                    <Form.Select
                                        value={resigningPrincipalId}
                                        onChange={(e) => setResigningPrincipalId(e.target.value)}
                                        disabled={isSaving}
                                    >
                                        {principalMembers.map((member: any) => (
                                            <option key={member.residentId} value={member.residentId}>
                                                {getResidentDisplay(member)} - {member.title || 'Üye'}
                                            </option>
                                        ))}
                                    </Form.Select>
                                </Form.Group>
                            </div>
                            <div className="col-md-6">
                                <Form.Group>
                                    <Form.Label className="fw-semibold">Asil Olacak Yedek Üye</Form.Label>
                                    <Form.Select
                                        value={promotingSubstituteId}
                                        onChange={(e) => setPromotingSubstituteId(e.target.value)}
                                        disabled={isSaving}
                                    >
                                        {substituteMembers.map((member: any) => (
                                            <option key={member.residentId} value={member.residentId}>
                                                {getResidentDisplay(member)}
                                            </option>
                                        ))}
                                    </Form.Select>
                                </Form.Group>
                            </div>
                        </div>

                        {selectedResigningPrincipal && selectedPromotingSubstitute && (
                            <div className="table-responsive border rounded mt-3">
                                <Table className="mb-0 align-middle">
                                    <tbody>
                                        <tr>
                                            <td className="ps-3 text-muted">Listeden çıkarılacak asil üye</td>
                                            <td className="fw-medium">{getResidentDisplay(selectedResigningPrincipal)}</td>
                                        </tr>
                                        <tr>
                                            <td className="ps-3 text-muted">Asil üyeliğe geçirilecek yedek</td>
                                            <td className="fw-medium">{getResidentDisplay(selectedPromotingSubstitute)}</td>
                                        </tr>
                                        <tr>
                                            <td className="ps-3 text-muted">Yeni görev / ünvan</td>
                                            <td className="fw-medium">{getPromotedTitle(selectedResigningPrincipal.title)}</td>
                                        </tr>
                                    </tbody>
                                </Table>
                            </div>
                        )}
                    </Modal.Body>
                    <Modal.Footer>
                        <Button variant="light" onClick={closeSubstitutePromotion} disabled={isSaving}>
                            Vazgeç
                        </Button>
                        <Button variant="warning" className="text-white" onClick={confirmSubstitutePromotion} disabled={isSaving}>
                            {isSaving ? (
                                <>
                                    <span className="spinner-border spinner-border-sm me-1" />
                                    Kaydediliyor...
                                </>
                            ) : (
                                'Yedekten Asil Ata'
                            )}
                        </Button>
                    </Modal.Footer>
                </Modal>
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
                    border-color: var(--bs-primary);
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

const MemberRow = memo(({ member, idx, residents, assignedResidentIds = [], updateMember, removeMember, disabled = false }: any) => {
    const isPassive = member.isActive === false;
    const rowDisabled = disabled || isPassive;
    
    return (
        <tr className={isPassive ? "opacity-50 bg-light" : ""}>
            <td className="ps-4">
                <div className="d-flex align-items-center gap-3">
                    <div className={`avatar-sm ${isPassive ? 'bg-secondary' : 'bg-soft-primary text-primary'} rounded-circle d-flex align-items-center justify-content-center fw-bold`}>
                        {member.residentId ? residents.find((r: any) => r.id === member.residentId)?.firstName?.charAt(0) : '?'}
                    </div>
                    <Form.Select
                        size="sm"
                        className={`member-select border-0 ${isPassive ? 'bg-transparent text-muted' : 'bg-light'}`}
                        value={member.residentId}
                        onChange={(e) => updateMember(idx, 'residentId', e.target.value)}
                        disabled={rowDisabled}
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
                    className={`border-0 ${isPassive ? 'bg-transparent text-muted' : 'bg-light'}`}
                    value={member.memberType}
                    disabled={rowDisabled}
                    onChange={(e) => {
                        const newType = Number(e.target.value)
                        updateMember(idx, 'memberType', newType)
                        if (newType === BoardMemberType.Substitute) {
                            updateMember(idx, 'title', 'Yedek Üye')
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
                    className={`border-0 ${isPassive ? 'bg-transparent text-muted text-decoration-line-through' : 'bg-light'}`}
                    value={member.title || ''}
                    onChange={(e) => updateMember(idx, 'title', e.target.value)}
                    disabled={rowDisabled || member.memberType === BoardMemberType.Substitute}
                >
                    {member.memberType === BoardMemberType.Substitute ? (
                        <option value="Yedek Üye">Yedek Üye</option>
                    ) : (
                        <>
                            <option value="">Seçiniz...</option>
                            <option value="Başkan">Başkan</option>
                            <option value="Başkan Yardımcısı">Başkan Yardımcısı</option>
                            <option value="Muhasip Üye">Muhasip Üye</option>
                            <option value="Üye">Üye</option>
                        </>
                    )}
                </Form.Select>
            </td>
            <td className="text-end pe-4 print-hide">
                {isPassive ? (
                    <Badge bg="secondary">İstifa Etti</Badge>
                ) : disabled ? (
                    <Badge bg="secondary">Kilitli</Badge>
                ) : (
                    <Button variant="link" className="text-danger p-0" onClick={() => removeMember(idx)}>
                        <LuTrash2 />
                    </Button>
                )}
            </td>
        </tr>
    )
})

export default ManagementBoardPage
