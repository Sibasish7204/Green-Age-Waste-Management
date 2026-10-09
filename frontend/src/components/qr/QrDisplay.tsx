import { QRCodeSVG } from 'qrcode.react'

export default function QrDisplay({ payload }: { payload: string }) {
  return <div className="qr-display"><QRCodeSVG value={payload} size={220} level="M" includeMargin /></div>
}