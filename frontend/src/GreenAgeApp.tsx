import { createContext, useContext, useState, type FormEvent, type ReactNode } from 'react'
import { Link, Navigate, Outlet, Route, Routes, useLocation, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import toast from 'react-hot-toast'
import { Bell, Check, ChevronRight, Leaf, LocateFixed, LogOut, MapPin, Phone, QrCode, Recycle, ShieldAlert, Sprout, Truck, UserRound, X } from 'lucide-react'
import { errorMessage } from './api/client'
import { categoriesApi, meApi, notificationsApi, requestsApi, sosApi } from './api/resources'
import { AuthProvider } from './context/AuthContext'
import { useAuth } from './context/useAuth'
import LocationPicker, { type LocationValue } from './components/map/LocationPicker'
import RequestCard from './components/request/RequestCard'
import StatusBadge from './components/request/StatusBadge'
import QrDisplay from './components/qr/QrDisplay'
import NonBioTrade from './components/sales/NonBioTrade'
import type { PickupRequest, RequestDraft, UserRole } from './types'
import './GreenAge.css'
import './App.css'
import './LandingBioLink.css'

const DraftContext = createContext<{ draft: RequestDraft | null; setDraft: (draft: RequestDraft | null) => void } | null>(null)
function useDraft() { const context = useContext(DraftContext); if (!context) throw new Error('Draft context missing'); return context }
const home = (role?: UserRole) => role === 'Collect' ? '/collect/inbox' : '/deliver/sell'
const bioAppUrl = import.meta.env.VITE_BIO_APP_URL || 'http://localhost:5174'

export default function GreenAgeApp() {
  const [draft, setDraft] = useState<RequestDraft | null>(null)
  return <AuthProvider><DraftContext.Provider value={{ draft, setDraft }}><Routes>
    <Route path="/" element={<Landing />} /><Route path="/login" element={<AuthPage register={false} />} /><Route path="/register" element={<AuthPage register />} />
    <Route element={<RequireAuth />}><Route element={<Shell />}>
      <Route path="/deliver/sell" element={<RequireRole role="Deliver"><NonBioTrade /></RequireRole>} />
      <Route path="/deliver/history" element={<RequireRole role="Deliver"><NonBioTrade initialView="history" /></RequireRole>} />
      <Route path="/collect/sales" element={<RequireRole role="Collect"><NonBioTrade initialView="scan" /></RequireRole>} />
      <Route path="/deliver/new" element={<RequireRole role="Deliver"><RaiseTicket /></RequireRole>} />
      <Route path="/deliver/cart" element={<RequireRole role="Deliver"><ServiceCart /></RequireRole>} />
      <Route path="/deliver/requests" element={<RequireRole role="Deliver"><MyRequests /></RequireRole>} />
      <Route path="/deliver/requests/:id" element={<RequireRole role="Deliver"><RequestDetail /></RequireRole>} />
      <Route path="/collect/inbox" element={<RequireRole role="Collect"><Inbox /></RequireRole>} />
      <Route path="/collect/requests/:id" element={<RequireRole role="Collect"><CollectDetail /></RequireRole>} />
      <Route path="/collect/mine" element={<RequireRole role="Collect"><MyRequests /></RequireRole>} />
      <Route path="/profile" element={<Profile />} /><Route path="/notifications" element={<Notifications />} />
    </Route></Route><Route path="*" element={<Navigate to="/" replace />} />
  </Routes></DraftContext.Provider></AuthProvider>
}

function RequireAuth() { const { auth } = useAuth(); return auth ? <Outlet /> : <Navigate to="/login" replace /> }
function RequireRole({ role, children }: { role: UserRole; children: ReactNode }) { const { auth } = useAuth(); return auth?.user.role === role ? children : <Navigate to={home(auth?.user.role)} replace /> }
function Logo() { return <Link className="brand" to="/"><span className="brand-mark"><Leaf size={24} /><Leaf size={18} /></span><span><b>GREEN AGE</b><small>DEVELOPMENT CORPORATION</small></span></Link> }

function Landing() {
  const { auth } = useAuth()
  if (auth) return <Navigate to={home(auth.user.role)} replace />
  return <div className="landing"><header className="landing-nav"><Logo /><Link to="/login">Log in <ChevronRight size={15} /></Link></header><main className="landing-main"><section className="landing-copy"><span className="eyebrow"><Sprout size={15} /> A cleaner way to move waste</span><h1>We think for<br /><em>every waste.</em></h1><p>Good waste deserves a better next step. Connect with local collectors and keep useful materials in motion.</p><div className="landing-choices"><Link className="choice choice-deliver" to="/register?role=Deliver"><span><Leaf size={22} /></span><strong>Sell recyclables</strong><small>Compare local kabadiwala rates</small><ChevronRight size={18} /></Link><Link className="choice choice-collect" to="/register?role=Collect"><span><Truck size={22} /></span><strong>Collect recyclables</strong><small>Scan sale QR codes</small><ChevronRight size={18} /></Link><a className="choice choice-bio" href={bioAppUrl} target="_blank" rel="noreferrer"><span><Sprout size={22} /></span><strong>Municipal bio collection</strong><small>Household QR and ward worker visits</small><ChevronRight size={18} /></a></div><div className="landing-login">Already part of the loop? <Link to="/login">Log in <ChevronRight size={15} /></Link></div></section><aside className="landing-art"><div className="sun-disc" /><div className="park-hill hill-one" /><div className="park-hill hill-two" /><div className="tree tree-one"><i /><b /><span /></div><div className="tree tree-two"><i /><b /><span /></div><div className="bin-illustration"><Recycle size={38} /><span>RECYCLE</span></div><div className="art-caption">Small actions.<br /><b>A lighter footprint.</b></div></aside></main><footer className="landing-footer"><span>GREEN AGE DEVELOPMENT CORPORATION</span><span>Collect well. Live well.</span></footer></div>
}

function AuthPage({ register }: { register: boolean }) {
  const { login, register: createAccount } = useAuth()
  const navigate = useNavigate(), location = useLocation()
  const preferred = new URLSearchParams(location.search).get('role')
  const [role, setRole] = useState<UserRole>(preferred === 'Collect' ? 'Collect' : 'Deliver')
  const [busy, setBusy] = useState(false)
  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); const form = Object.fromEntries(new FormData(event.currentTarget).entries()); setBusy(true)
    try {
      const user = register
        ? await createAccount({ name: String(form.name), email: String(form.email), password: String(form.password), phone: String(form.phone ?? ''), role })
        : await login(String(form.email), String(form.password))
      const from = (location.state as { from?: string } | null)?.from
      navigate(from || home(user.role), { replace: true })
    } catch (error) { toast.error(errorMessage(error)) } finally { setBusy(false) }
  }
  return <div className="auth-layout"><aside className="auth-aside"><Logo /><div><span className="eyebrow">Green Age community</span><h1>Waste has<br />another life.</h1><p>One pickup at a time, we make that next step easier.</p></div><span className="auth-aside-foot">WE THINK FOR EVERY WASTE.</span></aside><main className="auth-main"><Link className="back-link" to="/">← Home</Link><form className="auth-form" onSubmit={(event) => void submit(event)}><span className="eyebrow">{register ? 'Join the network' : 'Welcome back'}</span><h2>{register ? 'Create your account' : 'Sign in to Green Age'}</h2><p>{register ? 'A few details to get your pickups moving.' : 'Pick up where you left off.'}</p>{register && <><label>Full name<input name="name" required autoComplete="name" /></label><label>Phone number<input name="phone" type="tel" /></label><div className="field-label">I am here to</div><div className="role-switch"><button type="button" className={role === 'Deliver' ? 'selected' : ''} onClick={() => setRole('Deliver')}><Leaf size={16} /> Deliver</button><button type="button" className={role === 'Collect' ? 'selected' : ''} onClick={() => setRole('Collect')}><Truck size={16} /> Collect</button></div></>}<label>Email address<input name="email" type="email" required autoComplete="email" /></label><label>Password<input name="password" type="password" required minLength={register ? 8 : undefined} autoComplete={register ? 'new-password' : 'current-password'} /></label>{!register && <div className="demo-hint"><strong>Demo access</strong><span>deliver@demo.com or collector@demo.com</span><span>Password: Demo@123</span></div>}<button className="button button-primary full-button" disabled={busy}>{busy ? 'Please wait…' : register ? 'Create account' : 'Log in'}<ChevronRight size={17} /></button><div className="auth-switch">{register ? 'Already have an account?' : 'New to Green Age?'} <Link to={register ? '/login' : `/register?role=${role}`}>{register ? 'Log in' : 'Create an account'}</Link></div></form></main></div>
}

function Shell() {
  const { auth, logout } = useAuth(); const [sosOpen, setSosOpen] = useState(false), [alertsOpen, setAlertsOpen] = useState(false)
  const queryClient = useQueryClient(), notices = useQuery({ queryKey: ['notifications'], queryFn: notificationsApi.list, refetchInterval: 15_000 })
  const unread = notices.data?.filter((notice) => !notice.isRead).length ?? 0
  const title = location.pathname.includes('/sell') ? 'Sell recyclables' : location.pathname.includes('/history') ? 'Sale history' : location.pathname.includes('/sales') ? 'Verify a sale' : location.pathname.includes('/new') ? 'New pickup' : location.pathname.includes('/cart') ? 'Service cart' : location.pathname.includes('inbox') ? 'Request received' : location.pathname.includes('profile') ? 'Your profile' : location.pathname.includes('notifications') ? 'Notifications' : 'Pickup activity'
  const submitSos = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); const message = String(new FormData(event.currentTarget).get('message') ?? '')
    const send = (lat: number, lng: number) => sosApi(lat, lng, message).then(() => toast.success('Alert sent')).catch((error: unknown) => toast.error(errorMessage(error)))
    navigator.geolocation.getCurrentPosition(({ coords }) => void send(coords.latitude, coords.longitude), () => void send(0, 0))
    setSosOpen(false)
  }
  const read = async (id: string, requestId?: string | null) => { await notificationsApi.markRead(id); await queryClient.invalidateQueries({ queryKey: ['notifications'] }); setAlertsOpen(false); if (requestId) window.location.assign(`${auth?.user.role === 'Collect' ? '/collect/requests' : '/deliver/requests'}/${requestId}`) }
  return <div className="app-background"><header className="topbar"><Logo /><span className="page-pill">{title}</span><nav className="top-actions"><button className="sos-button" onClick={() => setSosOpen(true)}><ShieldAlert size={17} /> SOS</button><div className="notification-wrap"><button className="icon-button" aria-label="Notifications" onClick={() => setAlertsOpen(!alertsOpen)}><Bell size={20} />{unread > 0 && <span className="unread-dot">{unread}</span>}</button>{alertsOpen && <div className="notification-popover"><strong>Updates</strong>{notices.data?.slice(0, 5).map((notice) => <button key={notice.id} className="notification-item" onClick={() => void read(notice.id, notice.requestId)}><b>{notice.title}</b><small>{notice.message}</small></button>)}<Link to="/notifications">View all updates</Link></div>}</div><Link className="avatar-link" to="/profile">{auth?.user.name[0]}<span>{auth?.user.name.split(' ')[0]}</span></Link><button className="icon-button" aria-label="Log out" onClick={() => { logout(); window.location.assign('/') }}><LogOut size={17} /></button></nav></header><main className="main-content"><Outlet /></main><details className="contact-widget"><summary><Phone size={17} /> Contact us</summary><div><strong>Green Age support</strong><a href="tel:+919876543210">+91 98765 43210</a><a href="mailto:hello@greenage.demo">hello@greenage.demo</a></div></details>{sosOpen && <div className="modal-backdrop"><form className="modal" onSubmit={submitSos}><button className="modal-close" type="button" onClick={() => setSosOpen(false)} aria-label="Close"><X size={18} /></button><span className="eyebrow red-text">Emergency support</span><h2>Send an alert?</h2><p>Your location will be shared with your pickup partner.</p><label>Message (optional)<textarea name="message" /></label><div className="modal-actions"><button className="button button-quiet" type="button" onClick={() => setSosOpen(false)}>Cancel</button><button className="button button-red"><ShieldAlert size={17} /> Send SOS</button></div></form></div>}</div>
}

function Heading({ eyebrow, title, description, action }: { eyebrow: string; title: string; description: string; action?: ReactNode }) { return <div className="page-heading"><div><span className="eyebrow">{eyebrow}</span><h1>{title}</h1><p>{description}</p></div>{action}</div> }
function RaiseTicket() {
  const { draft, setDraft } = useDraft(), navigate = useNavigate()
  const [location, setLocation] = useState<LocationValue | null>(draft ? { latitude: draft.latitude, longitude: draft.longitude, address: draft.address } : null)
  const [selected, setSelected] = useState<number[]>(draft?.selectedCategoryIds ?? [])
  const categories = useQuery({ queryKey: ['categories'], queryFn: categoriesApi, staleTime: 3_600_000 })
  const toggle = (id: number) => setSelected((ids) => ids.includes(id) ? ids.filter((item) => item !== id) : [...ids, id])
  const proceed = () => { if (location?.address.trim() && selected.length) { setDraft({ ...location, selectedCategoryIds: selected }); navigate('/deliver/cart') } }
  return <><Heading eyebrow="Deliver · Step 1 of 2" title="Raise a ticket" description="Choose a pickup point and the materials that need a new home." /><section className="workflow-section"><div className="section-title"><span className="step-number">01</span><div><h2>Pickup location</h2><p>Pin where the waste is ready.</p></div></div><LocationPicker value={location} onChange={setLocation} /></section><section className="workflow-section"><div className="section-title"><span className="step-number">02</span><div><h2>What are we collecting?</h2><p>Select all materials included in this pickup.</p></div></div>{categories.isLoading ? <p>Loading categories…</p> : <div className="category-grid">{(['Bio', 'NonBio'] as const).map((group) => { const items = (categories.data ?? []).filter((item) => item.group === group); return <div className={`category-panel ${group === 'Bio' ? 'bio-panel' : 'nonbio-panel'}`} key={group}><div className="category-panel-heading"><span>{group === 'Bio' ? <Leaf size={18} /> : <Recycle size={18} />}</span><div><h3>{group === 'Bio' ? 'Bio' : 'Non-bio'}</h3><small>{items.length} material types</small></div></div>{items.map((item) => <label className="category-option" key={item.id}><input type="checkbox" checked={selected.includes(item.id)} onChange={() => toggle(item.id)} /><span className="category-dot"><Leaf size={13} /></span><span>{item.name}</span><small>{item.unit}</small></label>)}</div> })}</div>}</section><div className="workflow-footer"><span>{selected.length} materials selected</span><button className="button button-primary" disabled={!location?.address.trim() || !selected.length} onClick={proceed}>Continue to quantities <ChevronRight size={17} /></button></div></>
}

function ServiceCart() {
  const { draft, setDraft } = useDraft(), navigate = useNavigate(), cache = useQueryClient()
  const [amounts, setAmounts] = useState<Record<number, string>>({}), [notes, setNotes] = useState('')
  const categories = useQuery({ queryKey: ['categories'], queryFn: categoriesApi, staleTime: 3_600_000 })
  const create = useMutation({ mutationFn: requestsApi.create, onSuccess: async (request) => { await cache.invalidateQueries({ queryKey: ['requests'] }); setDraft(null); toast.success('Pickup request created'); navigate(`/deliver/requests/${request.id}`) }, onError: (error: unknown) => toast.error(errorMessage(error)) })
  if (!draft) return <Navigate to="/deliver/new" replace />
  const selected = (categories.data ?? []).filter((category) => draft.selectedCategoryIds.includes(category.id))
  const valid = selected.length > 0 && selected.every((category) => { const quantity = Number(amounts[category.id]); return quantity > 0 && quantity <= 10000 && Number.isFinite(quantity) && Math.round(quantity * 100) === quantity * 100 })
  return <><Heading eyebrow="Deliver · Step 2 of 2" title="Service cart" description="Enter an amount for each material and add useful pickup notes." action={<Link className="text-button" to="/deliver/new">Change location</Link>} /><div className="cart-layout"><section className="workflow-section"><div className="section-title"><span className="step-number">01</span><div><h2>Selected items</h2><p>{selected.length} material types</p></div></div>{selected.map((item) => <div className="cart-item" key={item.id}><span className="material-icon"><Leaf size={16} /></span><span>{item.name}</span><small>per {item.unit}</small></div>)}<label className="notes-field">Notes for the collector<textarea value={notes} onChange={(event) => setNotes(event.target.value)} placeholder="Gate, floor, or helpful directions" /></label></section><section className="quantity-panel"><span className="eyebrow">02 · Your quantities</span><h2>Total quantity</h2>{selected.map((item) => <label className="quantity-row" key={item.id}><span>{item.name}</span><span className="quantity-input"><input type="number" min="0.01" max="10000" step="0.01" value={amounts[item.id] ?? ''} aria-label={`${item.name} quantity`} onChange={(event) => setAmounts((current) => ({ ...current, [item.id]: event.target.value }))} />{item.unit}</span></label>)}<div className="cart-total"><span>Pickup address</span><strong>{draft.address}</strong></div><button className="button button-dark full-button" disabled={!valid || create.isPending} onClick={() => create.mutate({ latitude: draft.latitude, longitude: draft.longitude, address: draft.address, notes, items: selected.map((item) => ({ categoryId: item.id, quantity: Number(amounts[item.id]) })) })}>{create.isPending ? 'Sending…' : 'Send pickup request'}<ChevronRight size={17} /></button></section></div></>
}

function MyRequests() {
  const { auth } = useAuth(), role = auth!.user.role
  const query = useQuery({ queryKey: ['requests', 'mine'], queryFn: requestsApi.mine, refetchInterval: 10_000 })
  return <><Heading eyebrow={role === 'Deliver' ? 'Your pickups' : 'Assigned work'} title={role === 'Deliver' ? 'My requests' : 'My collection jobs'} description="Follow every pickup from request to completion." action={role === 'Deliver' ? <Link className="button button-primary" to="/deliver/new">New pickup <ChevronRight size={16} /></Link> : undefined} />{query.isLoading ? <p>Loading pickup history…</p> : query.data?.length ? <div className="request-list">{query.data.map((request) => <RequestCard key={request.id} request={request} role={role} />)}</div> : <Empty title="No requests yet" text={role === 'Deliver' ? 'Raise your first pickup ticket when waste is ready.' : 'Accepted collection jobs will appear here.'} action={role === 'Deliver' ? <Link className="button button-primary" to="/deliver/new">Raise a ticket</Link> : undefined} />}</>
}

function RequestDetail() {
  const { id = '' } = useParams(), cache = useQueryClient(), query = useQuery({ queryKey: ['requests', id], queryFn: () => requestsApi.get(id) })
  const [qr, setQr] = useState<{ payload: string; expiresAt: string } | null>(null), [reason, setReason] = useState('')
  const cancel = useMutation({ mutationFn: () => requestsApi.cancel(id, reason), onSuccess: async () => { toast.success('Request cancelled'); await cache.invalidateQueries({ queryKey: ['requests'] }); await query.refetch() }, onError: (error: unknown) => toast.error(errorMessage(error)) })
  if (!query.data) return <Loading error={query.error} />
  const request = query.data
  const getQr = async () => { try { const result = await requestsApi.qr(id); setQr(result) } catch (error) { toast.error(errorMessage(error)) } }
  return <><Heading eyebrow="Pickup detail" title="Your request" description={`Created ${new Date(request.createdAt).toLocaleString()}`} action={<StatusBadge status={request.status} />} /><RequestSummary request={request} />{request.status === 'Accepted' && <section className="qr-panel"><div><span className="eyebrow">Secure handover</span><h2>Show your QR</h2><p>Allow your collector to scan this code at pickup.</p>{qr && <p>Expires {new Date(qr.expiresAt).toLocaleTimeString()}</p>}<button className="button button-primary" onClick={() => void getQr()}>{qr ? 'Refresh QR' : 'Generate QR'}</button></div>{qr ? <QrDisplay payload={qr.payload} /> : <div className="qr-placeholder"><ShieldAlert size={28} />QR not generated</div>}</section>}{['Requested', 'Accepted'].includes(request.status) && <section className="cancel-strip"><label>Cancel reason<input value={reason} onChange={(event) => setReason(event.target.value)} placeholder="Optional reason" /></label><button className="button button-outline-red" onClick={() => cancel.mutate()}>Cancel request</button></section>}<Link className="back-link" to="/deliver/requests">← All requests</Link></>
}

function Inbox() {
  const [radius, setRadius] = useState(10), [coords, setCoords] = useState<{ lat: number; lng: number } | null>(null)
  const query = useQuery({ queryKey: ['requests', 'open', radius, coords], queryFn: () => requestsApi.open(coords?.lat, coords?.lng, radius), refetchInterval: 10_000 })
  const locate = () => navigator.geolocation.getCurrentPosition(({ coords: point }) => setCoords({ lat: point.latitude, lng: point.longitude }), () => toast.error('Location unavailable. Showing newest requests.'))
  return <><Heading eyebrow="Collect · Nearby pickups" title="Request received" description="Open pickups sorted by distance when your location is available." action={<div className="inbox-heading-actions"><Link className="button button-outline" to="/collect/sales"><QrCode size={16} /> Scan sale QR</Link><button className="button button-outline" onClick={locate}><LocateFixed size={17} /> Find near me</button></div>} /><div className="inbox-controls"><span>{coords ? 'Nearby pickups' : 'Latest open pickups'}</span><label>Radius <select value={radius} onChange={(event) => setRadius(Number(event.target.value))}><option value={5}>5 km</option><option value={10}>10 km</option><option value={25}>25 km</option><option value={50}>50 km</option></select></label></div>{query.data?.length ? <div className="request-list">{query.data.map((request) => <RequestCard key={request.id} request={request} role="Collect" />)}</div> : <Empty title="No open pickups nearby" text="Check back soon or expand your search radius." />}</>
}

function CollectDetail() {
  const { id = '' } = useParams(), cache = useQueryClient(), query = useQuery({ queryKey: ['requests', id], queryFn: () => requestsApi.get(id) })
  const [token, setToken] = useState(''), [actual, setActual] = useState<Record<number, string>>({})
  const update = async (fn: () => Promise<unknown>, success: string) => { try { await fn(); toast.success(success); await cache.invalidateQueries({ queryKey: ['requests'] }); await query.refetch() } catch (error) { toast.error(errorMessage(error)); if (errorMessage(error).includes('already accepted')) window.location.assign('/collect/inbox') } }
  if (!query.data) return <Loading error={query.error} />
  const request = query.data
  return <><Heading eyebrow="Collection detail" title={request.status === 'Accepted' ? 'Scan deliverer QR' : request.status === 'Verified' ? 'Process request' : 'Pickup details'} description={request.address} action={<StatusBadge status={request.status} />} />{request.status === 'Requested' && <div className="proceed-banner"><div><span className="eyebrow">Available pickup</span><h2>Ready to collect?</h2><p>Accepting assigns this pickup to you.</p></div><button className="button button-primary" onClick={() => void update(() => requestsApi.accept(id), 'Request accepted')}>Proceed <ChevronRight size={17} /></button></div>}{request.status === 'Accepted' && <section className="workflow-section scan-panel"><span className="eyebrow">Verify handover</span><h2>Enter the deliverer’s QR token</h2><p>Scan the displayed code with a QR app, then enter the token value.</p><label>QR token<input value={token} onChange={(event) => setToken(event.target.value.trim())} placeholder="Paste token" /></label><button className="button button-primary" disabled={!token} onClick={() => void update(() => requestsApi.verify(id, token), 'Handover verified')}>Verify code <Check size={17} /></button><p><a target="_blank" rel="noreferrer" href={`https://www.google.com/maps/dir/?api=1&destination=${request.latitude},${request.longitude}`}>Open in Google Maps <ChevronRight size={15} /></a></p><button className="text-button danger-text" onClick={() => void update(() => requestsApi.cancel(id, 'Collector released the pickup'), 'Pickup released')}>Cancel job</button></section>}{request.status === 'Verified' && <section className="workflow-section"><span className="eyebrow">Record collected amounts</span><h2>Actual quantities</h2>{request.items.map((item) => <label className="quantity-row" key={item.categoryId}><span>{item.categoryName}<small>Requested: {item.requestedQuantity} {item.unit}</small></span><span className="quantity-input"><input type="number" min="0.01" step="0.01" value={actual[item.categoryId] ?? item.requestedQuantity} onChange={(event) => setActual((current) => ({ ...current, [item.categoryId]: event.target.value }))} />{item.unit}</span></label>)}<button className="button button-primary" onClick={() => void update(() => requestsApi.complete(id, request.items.map((item) => ({ categoryId: item.categoryId, actualQuantity: Number(actual[item.categoryId] ?? item.requestedQuantity) }))), 'Pickup completed')}>Complete pickup <Check size={17} /></button></section>}<RequestSummary request={request} /><Link className="back-link" to="/collect/inbox">← Back to inbox</Link></>
}

function RequestSummary({ request }: { request: PickupRequest }) {
  return <section className="request-summary"><div className="summary-address"><MapPin size={19} /><div><strong>{request.address}</strong>{request.notes && <p>{request.notes}</p>}</div></div><div className="summary-items">{request.items.map((item) => <div key={item.categoryId}><span>{item.categoryName}</span><strong>{item.requestedQuantity} {item.unit}{item.actualQuantity != null && <small> · actual {item.actualQuantity}</small>}</strong></div>)}</div><div className="summary-parties"><span>Deliverer <strong>{request.deliver.name}</strong>{request.deliver.phone && <a href={`tel:${request.deliver.phone}`}>{request.deliver.phone}</a>}</span>{request.collector && <span>Collector <strong>{request.collector.name}</strong>{request.collector.phone && <a href={`tel:${request.collector.phone}`}>{request.collector.phone}</a>}</span>}</div><div className="timeline"><span className="timeline-done">Requested</span><span className={request.acceptedAt ? 'timeline-done' : ''}>Accepted</span><span className={request.verifiedAt ? 'timeline-done' : ''}>Verified</span><span className={request.completedAt ? 'timeline-done' : ''}>Completed</span></div></section>
}

function Profile() {
  const { auth, saveUser, logout } = useAuth(), query = useQuery({ queryKey: ['me'], queryFn: meApi.get })
  const update = useMutation({ mutationFn: meApi.update, onSuccess: (user) => { saveUser(user); toast.success('Profile saved') }, onError: (error: unknown) => toast.error(errorMessage(error)) })
  const user = query.data ?? auth!.user
  return <><Heading eyebrow="Account" title="Your profile" description="Keep your contact details current." /><form className="profile-form" onSubmit={(event) => { event.preventDefault(); const data = new FormData(event.currentTarget); update.mutate({ name: String(data.get('name')), phone: String(data.get('phone') ?? ''), photoUrl: String(data.get('photoUrl') ?? '') }) }}><div className="profile-avatar"><UserRound size={28} /></div><label>Name<input name="name" defaultValue={user.name} required /></label><label>Email<input value={user.email} readOnly /></label><label>Phone<input name="phone" defaultValue={user.phone ?? ''} /></label><label>Photo URL<input name="photoUrl" type="url" defaultValue={user.photoUrl ?? ''} /></label><label>Role<input readOnly value={user.role === 'Deliver' ? 'Deliverer' : 'Collector'} /></label><button className="button button-primary">Save profile <Check size={17} /></button><button type="button" className="text-button danger-text" onClick={() => { logout(); window.location.assign('/') }}>Log out</button></form></>
}

function Notifications() {
  const { auth } = useAuth(), cache = useQueryClient(), navigate = useNavigate()
  const query = useQuery({ queryKey: ['notifications'], queryFn: notificationsApi.list, refetchInterval: 15_000 })
  return <><Heading eyebrow="Your activity" title="Notifications" description="Updates about pickups and collection jobs." />{query.data?.length ? <div className="notification-list">{query.data.map((notice) => <button className={`notification-row ${notice.isRead ? '' : 'is-unread'}`} key={notice.id} onClick={() => { void notificationsApi.markRead(notice.id).then(() => cache.invalidateQueries({ queryKey: ['notifications'] })); if (notice.requestId) navigate(`${auth?.user.role === 'Collect' ? '/collect/requests' : '/deliver/requests'}/${notice.requestId}`) }}><span className="notification-mark"><Bell size={17} /></span><span><strong>{notice.title}</strong><small>{notice.message}</small></span><time>{new Date(notice.createdAt).toLocaleString()}</time></button>)}</div> : <Empty title="You’re all caught up" text="Pickup updates will appear here." />}</>
}

function Empty({ title, text, action }: { title: string; text: string; action?: ReactNode }) { return <section className="empty-state"><span><Sprout size={24} /></span><h2>{title}</h2><p>{text}</p>{action}</section> }
function Loading({ error }: { error: unknown }) { return error ? <Empty title="Could not load request" text={errorMessage(error)} /> : <p className="loading-line">Loading pickup details…</p> }
