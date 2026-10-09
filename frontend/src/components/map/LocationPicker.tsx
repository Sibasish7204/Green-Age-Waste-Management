import { useState } from 'react'
import { Autocomplete, GoogleMap, Marker, useJsApiLoader } from '@react-google-maps/api'
import { LocateFixed, MapPin } from 'lucide-react'

export interface LocationValue {
  latitude: number
  longitude: number
  address: string
}

interface Props {
  value: LocationValue | null
  onChange: (value: LocationValue) => void
}

export default function LocationPicker({ value, onChange }: Props) {
  const [center, setCenter] = useState({ lat: value?.latitude ?? 12.9716, lng: value?.longitude ?? 77.5946 })
  const [autocomplete, setAutocomplete] = useState<google.maps.places.Autocomplete | null>(null)
  const key = import.meta.env.VITE_GOOGLE_MAPS_API_KEY as string | undefined
  const { isLoaded, loadError } = useJsApiLoader({ id: 'greenage-maps', googleMapsApiKey: key || '', libraries: ['places'] })
  const update = (latitude: number, longitude: number, address = value?.address ?? '') => {
    setCenter({ lat: latitude, lng: longitude })
    onChange({ latitude, longitude, address })
  }
  const geolocate = () => navigator.geolocation.getCurrentPosition(
    ({ coords }) => update(coords.latitude, coords.longitude),
    () => undefined,
  )
  const reverseGeocode = (latitude: number, longitude: number) => {
    if (!window.google?.maps) return
    new window.google.maps.Geocoder().geocode({ location: { lat: latitude, lng: longitude } }, (results, status) => {
      update(latitude, longitude, status === 'OK' ? results?.[0]?.formatted_address ?? '' : '')
    })
  }
  const selectPlace = () => {
    const place = autocomplete?.getPlace()
    const position = place?.geometry?.location
    if (!position) return
    update(position.lat(), position.lng(), place.formatted_address ?? place.name ?? '')
  }

  if (key && isLoaded && !loadError) {
    return <div className="location-map">
      <GoogleMap mapContainerStyle={{ width: '100%', height: '260px' }} center={center} zoom={13}>
        <Marker position={center} draggable onDragEnd={(event) => event.latLng && reverseGeocode(event.latLng.lat(), event.latLng.lng())} />
      </GoogleMap>
      <div className="map-controls"><Autocomplete onLoad={setAutocomplete} onPlaceChanged={selectPlace}><input aria-label="Pickup address" value={value?.address ?? ''} placeholder="Search or enter pickup address" onChange={(event) => onChange({ ...center, latitude: center.lat, longitude: center.lng, address: event.target.value })} /></Autocomplete><button className="icon-button" title="Use my current location" onClick={geolocate}><LocateFixed size={18} /></button></div>
    </div>
  }

  return <div className="location-fallback">
    <div className="fallback-title"><MapPin size={18} /><span>Pickup location</span><button className="text-button" onClick={geolocate}><LocateFixed size={16} /> Use my location</button></div>
    <label>Address<input value={value?.address ?? ''} placeholder="Street, area, city" onChange={(event) => onChange({ latitude: value?.latitude ?? center.lat, longitude: value?.longitude ?? center.lng, address: event.target.value })} /></label>
    <div className="coordinates"><label>Latitude<input type="number" step="any" value={value?.latitude ?? ''} onChange={(event) => update(Number(event.target.value), value?.longitude ?? center.lng)} /></label><label>Longitude<input type="number" step="any" value={value?.longitude ?? ''} onChange={(event) => update(value?.latitude ?? center.lat, Number(event.target.value))} /></label></div>
    {key && loadError && <p className="muted">Map unavailable. Enter an address and coordinates to continue.</p>}
  </div>
}