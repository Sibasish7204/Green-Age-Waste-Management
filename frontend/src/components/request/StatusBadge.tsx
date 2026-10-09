import type { RequestStatus } from '../../types'

const labels: Record<RequestStatus, string> = {
  Requested: 'Requested', Accepted: 'Accepted', Verified: 'Verified', Completed: 'Completed', Cancelled: 'Cancelled',
}

export default function StatusBadge({ status }: { status: RequestStatus }) {
  return <span className={`status-badge status-${status.toLowerCase()}`}>{labels[status]}</span>
}