
import clsx from 'clsx'
import { Col, Row } from 'react-bootstrap'
import { TbChevronLeft, TbChevronRight } from 'react-icons/tb'
import { useTranslation } from 'react-i18next'

export type TablePaginationProps = {
    totalItems: number
    start: number
    end: number
    itemsName?: string
    showInfo?: boolean
    // Pagination control props
    previousPage: () => void
    canPreviousPage: boolean
    pageCount: number
    pageIndex: number
    setPageIndex: (index: number) => void
    nextPage: () => void
    canNextPage: boolean
    className?: string
}

const TablePagination = ({
    totalItems,
    start,
    end,
    itemsName = 'items',
    showInfo = true,
    previousPage,
    canPreviousPage,
    pageCount,
    pageIndex,
    setPageIndex,
    nextPage,
    canNextPage,
    className
}: TablePaginationProps) => {
    const { t } = useTranslation()

    // Smart pagination: show only nearby pages
    const getPageNumbers = () => {
        const pages: (number | string)[] = []
        const delta = 2 // Show 2 pages on each side of current page

        if (pageCount <= 7) {
            // Show all pages if total is small
            for (let i = 0; i < pageCount; i++) {
                pages.push(i)
            }
        } else {
            // Always show first page
            pages.push(0)

            // Calculate range around current page
            const rangeStart = Math.max(1, pageIndex - delta)
            const rangeEnd = Math.min(pageCount - 2, pageIndex + delta)

            // Add ellipsis after first page if needed
            if (rangeStart > 1) {
                pages.push('ellipsis-start')
            }

            // Add pages around current page
            for (let i = rangeStart; i <= rangeEnd; i++) {
                pages.push(i)
            }

            // Add ellipsis before last page if needed
            if (rangeEnd < pageCount - 2) {
                pages.push('ellipsis-end')
            }

            // Always show last page
            pages.push(pageCount - 1)
        }

        return pages
    }

    return (
        <Row className={clsx('align-items-center text-center text-sm-start', showInfo ? 'justify-content-between' : 'justify-content-end')}>
            {showInfo && (
                <Col sm>
                    <div className="text-muted">
                        {t('common.pagination.showing', { start, end, total: totalItems })} {itemsName}
                    </div>
                </Col>
            )}
            <Col sm="auto" className="mt-3 mt-sm-0">
                <div>
                    <ul className={clsx('pagination pagination-boxed mb-0 justify-content-center pagination-sm', className)}>
                        <li className="page-item">
                            <button className="page-link" onClick={() => previousPage()} disabled={!canPreviousPage}>
                                <TbChevronLeft />
                            </button>
                        </li>

                        {getPageNumbers().map((page, idx) => {
                            if (typeof page === 'string') {
                                // Ellipsis
                                return (
                                    <li key={page} className="page-item disabled">
                                        <span className="page-link">...</span>
                                    </li>
                                )
                            }
                            return (
                                <li key={idx} className={`page-item ${pageIndex === page ? 'active' : ''}`}>
                                    <button className="page-link" onClick={() => setPageIndex(page)}>
                                        {page + 1}
                                    </button>
                                </li>
                            )
                        })}

                        <li className="page-item">
                            <button className="page-link" onClick={() => nextPage()} disabled={!canNextPage}>
                                <TbChevronRight />
                            </button>
                        </li>
                    </ul>
                </div>
            </Col>
        </Row>
    )
}

export default TablePagination
