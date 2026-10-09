import { ArrowUpRight, MapPin } from 'lucide-react'
import { Link } from 'react-router-dom'
import type { PickupRequest } from '../../types'
import StatusBadge from './StatusBadge'

export default function RequestCard({ request, role }: { request: PickupRequest; role: 'Deliver' | 'Collect' }) {
  const path = role === 'Deliver' ? `/deliver/requests/${request.id}` : `/collect/requests/${request.id}`
  return <Link className="request-row" to={path}>
    <div className="request-row-main"><div className="request-row-title"><StatusBadge status={request.status} /><span>{request.items.map((item) => item.categoryName).join(', ')}</span></div><p><MapPin size={14} />{request.address}</p></div>
    <div className="request-row-meta"><span>{new Date(request.createdAt).toLocaleDateString()}</span>{request.distanceKm != null && <span>{request.distanceKm} km away</span>}<ArrowUpRight size={18} /></div>
  </Link>
}