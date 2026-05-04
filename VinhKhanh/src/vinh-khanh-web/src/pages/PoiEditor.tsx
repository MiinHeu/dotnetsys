import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate, useParams } from 'react-router'
import React, { useEffect, useState } from 'react'
import type { AxiosError } from 'axios'
import { api, type Poi } from '@/lib/api'
import { useAuthStore } from '@/store/authStore'

type PoiTranslation = NonNullable<Poi['translations']>[number]

type GeoPoint = {
  lat: number
  lon: number
  label: string
}

type TtsLanguage = {
  code: string
  label: string
  voice: string
  previewLang: string
}

const empty: Poi = {
  id: 0,
  name: '',
  description: '',
  ownerInfo: '',
  latitude: 10.7531,
  longitude: 106.678,
  mapX: 50,
  mapY: 50,
  triggerRadiusMeters: 15,
  priority: 5,
  cooldownSeconds: 10,
  qrCode: null,
  imageUrl: null,
  audioViUrl: null,
  category: 0,
  isActive: true,
  address: '',
  phoneNumber: '',
  operatingHours: '',
  rating: 5,
  imagesJson: '[]',
  menuJson: '[]',
  tagsJson: '[]',
  translations: [],
}

const ttsLanguages: TtsLanguage[] = [
  { code: 'vi', label: 'Tiếng Việt', voice: 'Chi', previewLang: 'vi-VN' },
  { code: 'en', label: 'Tiếng Anh', voice: 'Linda', previewLang: 'en-US' },
  { code: 'zh', label: 'Tiếng Trung', voice: 'Luli', previewLang: 'zh-CN' },
  { code: 'ko', label: 'Tiếng Hàn', voice: 'Nari', previewLang: 'ko-KR' },
  { code: 'ja', label: 'Tiếng Nhật', voice: 'Hina', previewLang: 'ja-JP' },
  { code: 'th', label: 'Tiếng Thái', voice: 'th-TH-PremwadeeNeural', previewLang: 'th-TH' },
]

function findTranslation(form: Poi, languageCode: string): PoiTranslation | undefined {
  return form.translations?.find((item) => item.languageCode === languageCode)
}

function upsertTranslation(
  form: Poi,
  languageCode: string,
  patch: Partial<PoiTranslation> & Pick<PoiTranslation, 'description'>,
  originalDescription?: string,
): Poi {
  const current = form.translations ?? []
  const existing = current.find((item) => item.languageCode === languageCode)

  if (existing) {
    return {
      ...form,
      translations: current.map((item) =>
        item.languageCode === languageCode
          ? {
            ...item,
            ...patch,
            originalDescription: originalDescription ?? item.originalDescription ?? form.description,
          }
          : item,
      ),
    }
  }

  return {
    ...form,
    translations: [
      ...current,
      {
        languageCode,
        name: patch.name ?? form.name,
        description: patch.description,
        audioUrl: patch.audioUrl ?? null,
        originalDescription: originalDescription ?? form.description,
      },
    ],
  }
}

async function geocodeAddress(address: string): Promise<GeoPoint> {
  const query = address.trim()
  if (!query) throw new Error('Vui lòng nhập địa chỉ trước.')

  const candidates = [
    query,
    `${query}, Vĩnh Khánh, Quận 4, TP.HCM, Việt Nam`,
    `${query}, Quận 4, TP.HCM, Việt Nam`,
  ]

  for (const candidate of candidates) {
    const url = new URL('https://nominatim.openstreetmap.org/search')
    url.searchParams.set('q', candidate)
    url.searchParams.set('format', 'jsonv2')
    url.searchParams.set('limit', '1')
    url.searchParams.set('addressdetails', '1')

    const res = await fetch(url.toString(), {
      headers: { Accept: 'application/json' },
    })

    if (!res.ok) continue

    const data = (await res.json()) as Array<{
      lat: string
      lon: string
      display_name: string
    }>

    if (data.length > 0) {
      return {
        lat: Number(data[0].lat),
        lon: Number(data[0].lon),
        label: data[0].display_name,
      }
    }
  }

  throw new Error('Không tìm thấy tọa độ từ địa chỉ này. Hãy nhập rõ hơn, ví dụ kèm Quận 4.')
}

async function translateText(text: string, language: TtsLanguage): Promise<string> {
  if (language.code === 'vi') return text

  const { data } = await api.post<{ translatedText?: string }>('/api/translation/text', {
    text,
    from: 'vi',
    to: language.code,
  })

  const translated = data?.translatedText?.trim()
  if (!translated) {
    throw new Error(`Không dịch được mô tả sang ${language.label}.`)
  }

  return translated
}

function extractApiErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof Error && err.message) {
    const maybeAxios = err as AxiosError<{ message?: string }>
    const apiMessage = maybeAxios.response?.data?.message
    if (apiMessage && apiMessage.trim()) return apiMessage.trim()
  }
  return fallback
}

export function PoiEditor() {
  const { id } = useParams()
  const isNew = id === 'new'
  const navigate = useNavigate()
  const qc = useQueryClient()
  const role = useAuthStore((s) => s.role)

  const [form, setForm] = useState<Poi>(empty)
  const [geoBusy, setGeoBusy] = useState(false)
  const [geoMessage, setGeoMessage] = useState('')
  const [ttsBusy, setTtsBusy] = useState(false)
  const [ttsMessage, setTtsMessage] = useState('')
  const [audioBusy, setAudioBusy] = useState(false)
  const [ownerMessage, setOwnerMessage] = useState('')
  const [previewBusy, setPreviewBusy] = useState(false)
  const [ttsLangCode, setTtsLangCode] = useState('vi')
  const [owners, setOwners] = useState<any[]>([])

  const isAdmin = role === 'Admin'

  useEffect(() => {
    if (isAdmin) {
      api.get('/api/auth/owners')
        .then(res => setOwners(res.data))
        .catch(err => console.error('Lỗi tải danh sách chủ quán:', err))
    }
  }, [isAdmin])

  const poiQ = useQuery({
    queryKey: ['poi', id],
    enabled: !isNew && id !== 'new' && !!id,
    queryFn: async () => (await api.get<Poi>(`/api/poi/${id}`)).data,
  })

  const ownersQ = useQuery({
    queryKey: ['owners'],
    enabled: role === 'Admin',
    queryFn: async () => (await api.get<{ id: number; username: string }[]>('/api/auth/owners')).data,
  })

  useEffect(() => {
    if (!poiQ.data) return

    setForm({
      ...poiQ.data,
      translations: poiQ.data.translations ?? [],
    })

    if (poiQ.data.ownerInfo?.trim()) {
      setGeoMessage(`Địa chỉ hiện tại: ${poiQ.data.ownerInfo}`)
    }
  }, [poiQ.data])

  useEffect(() => {
    if (role !== 'Admin') return
    if (!ownersQ.data || ownersQ.data.length === 0) return
    setForm((current) => {
      if (current.ownerUserId != null) return current
      return { ...current, ownerUserId: ownersQ.data[0].id }
    })
  }, [ownersQ.data, role])

  const resolveAddressToCoordinates = async () => {
    const address = form.ownerInfo?.trim() ?? ''
    if (!address) throw new Error('Vui lòng nhập địa chỉ / số nhà trước.')

    setGeoBusy(true)
    setGeoMessage('Đang lấy tọa độ từ địa chỉ...')
    try {
      const point = await geocodeAddress(address)
      setForm((current) => ({
        ...current,
        latitude: point.lat,
        longitude: point.lon,
      }))
      setGeoMessage(`Đã định vị: ${point.label}`)
      return point
    } finally {
      setGeoBusy(false)
    }
  }

  const save = useMutation({
    mutationFn: async () => {
      try {
        const address = form.ownerInfo?.trim() ?? ''
        if (!address) throw new Error('Vui lòng nhập địa chỉ / số nhà trước khi lưu.')

        let payload = form
        if (role === 'Admin') {
          if ((ownersQ.data?.length ?? 0) === 0) {
            setOwnerMessage('Chưa có tài khoản chủ quán. Hãy tạo Owner trước khi lưu POI.')
            throw new Error('Chưa có tài khoản chủ quán. Hãy tạo Owner trước khi lưu POI.')
          }
          if (!payload.ownerUserId) {
            setOwnerMessage('Vui lòng chọn chủ quán trước khi lưu.')
            throw new Error('Vui lòng chọn chủ quán trước khi lưu.')
          }
          setOwnerMessage('')
        }

        if (!Number.isFinite(form.latitude) || !Number.isFinite(form.longitude) || !address) {
          const point = await geocodeAddress(address)
          payload = {
            ...form,
            latitude: point.lat,
            longitude: point.lon,
          }
          setForm(payload)
          setGeoMessage(`Đã định vị: ${point.label}`)
        }

        if (isNew) {
          await api.post('/api/poi', payload)
        } else {
          if (!id) throw new Error('ID POI không hợp lệ')
          await api.put(`/api/poi/${id}`, payload)
        }
      } catch (err) {
        console.error('POI save error:', err)
        throw new Error(extractApiErrorMessage(err, 'Không lưu được POI. Vui lòng kiểm tra dữ liệu.'))
      }
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['pois'] })
      navigate('/pois')
    },
    onError: (err: Error) => {
      console.error('Mutation error:', err)
      const message = err.message || ''
      if (message.toLowerCase().includes('chủ quán') || message.toLowerCase().includes('owner')) {
        setOwnerMessage(message)
      }
    },
  })

  const prepareNarrationText = async (): Promise<{ language: TtsLanguage; text: string }> => {
    const language = ttsLanguages.find((item) => item.code === ttsLangCode) ?? ttsLanguages[0]
    const sourceText = form.description.trim()
    if (!sourceText) throw new Error('Vui lòng nhập mô tả trước.')

    let text = sourceText
    if (language.code !== 'vi') {
      const existingTranslation = findTranslation(form, language.code)
      // Kiểm tra nếu bản dịch cũ khớp với description hiện tại (chưa thay đổi)
      if (
        existingTranslation &&
        existingTranslation.description?.trim() &&
        existingTranslation.originalDescription === sourceText
      ) {
        // Dùng bản dịch cũ vì description chưa thay đổi
        text = existingTranslation.description
      } else {
        // Description thay đổi hoặc chưa có bản dịch → dịch lại
        setTtsMessage(`Đang dịch mô tả sang ${language.label}...`)
        text = await translateText(sourceText, language)
        const translatedName = await translateText(form.name.trim() || 'POI', language)
        setForm((current) =>
          upsertTranslation(current, language.code, {
            name: translatedName,
            description: text,
          }, sourceText), // Lưu originalDescription để so sánh sau
        )
      }
    }

    return { language, text }
  }

  const previewVoiceFromDescription = async () => {
    setPreviewBusy(true)
    try {
      const { language, text } = await prepareNarrationText()
      setTtsMessage(`Đang lấy giọng đọc ${language.label} từ VoiceRSS...`)

      const response = await api.post('/api/tts/synthesize', {
        text,
        lang: language.code,
        voice: language.voice
      }, { responseType: 'blob' })

      const blob = new Blob([response.data], { type: 'audio/mpeg' })
      const url = window.URL.createObjectURL(blob)
      const audio = new Audio(url)

      setTtsMessage(`Đang nghe thử ${language.label} (VoiceRSS)...`)
      await audio.play()
      setTtsMessage(`Đã nghe thử ${language.label}. Nếu ổn, bạn có thể bấm Xuất mp3.`)
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Không nghe thử được nội dung.'
      setTtsMessage(message)
      throw err
    } finally {
      setPreviewBusy(false)
    }
  }

  const generateVoiceFromDescription = async () => {
    setTtsBusy(true)
    try {
      const { language, text } = await prepareNarrationText()
      setTtsMessage(`Đang xuất file mp3 ${language.label}...`)

      const { data } = await api.post<{ url?: string; filename?: string }>('/api/audio/generate-tts', {
        text,
        lang: language.code,
        voice: language.voice,
      })

      if (!data?.url) {
        throw new Error('Backend không trả về URL audio.')
      }

      setForm((current) =>
        language.code === 'vi'
          ? { ...current, audioViUrl: data.url }
          : upsertTranslation(current, language.code, {
            name: current.name,
            description: findTranslation(current, language.code)?.description ?? text,
            audioUrl: data.url,
          }),
      )

      setTtsMessage(`Đã xuất audio ${language.label}. Tệp đã được gán cho quán ăn này và có thể quản lý tại mục "Quản lý Audio".`)
      try {
        setAudioBusy(true)
        const audio = new Audio(data.url)
        await audio.play()
      } catch {
        // Silent
      } finally {
        setAudioBusy(false)
      }
    } catch (err: unknown) {
      const message =
        typeof err === 'object' &&
          err !== null &&
          'response' in err &&
          typeof (err as { response?: { data?: { message?: string } } }).response?.data?.message === 'string'
          ? (err as { response?: { data?: { message?: string } } }).response!.data!.message!
          : (err as Error).message || 'Không xuất được voice từ mô tả.'
      setTtsMessage(message)
      throw err
    } finally {
      setTtsBusy(false)
    }
  }

  const generateVoiceForAllLangs = async () => {
    setTtsBusy(true)
    setTtsMessage('Bắt đầu dịch và tạo audio cho tất cả các ngôn ngữ...')
    try {
      const sourceText = form.description.trim()
      const sourceName = form.name.trim() || 'POI'
      if (!sourceText) throw new Error('Vui lòng nhập mô tả Tiếng Việt trước.')

      let currentForm = form
      for (const language of ttsLanguages) {
        setTtsMessage(`Đang xử lý ${language.label}...`)

        // 1. Translate
        let text = sourceText
        let name = sourceName
        if (language.code !== 'vi') {
          text = await translateText(sourceText, language)
          name = await translateText(sourceName, language)
        }

        // 2. Generate Audio
        setTtsMessage(`Đang tạo audio ${language.label}...`)
        const { data } = await api.post<{ url?: string; filename?: string }>('/api/audio/generate-tts', {
          text,
          lang: language.code,
          voice: language.voice,
        })

        if (!data?.url) {
          throw new Error(`Đã xảy ra lỗi lấy audio từ backend cho ${language.code}.`)
        }

        // 3. Update Form
        if (language.code === 'vi') {
          currentForm = { ...currentForm, audioViUrl: data.url }
        } else {
          currentForm = upsertTranslation(currentForm, language.code, {
            name: name,
            description: text,
            audioUrl: data.url,
          }, sourceText)
        }
      }

      setForm(currentForm)
      setTtsMessage('Đã hoàn tất! Toàn bộ audio đa ngôn ngữ đã được cập nhật và sẵn sàng trong trang Quản lý Audio.')
    } catch (err: unknown) {
      setTtsMessage('Lỗi khi tạo hàng loạt: ' + (err instanceof Error ? err.message : 'Unknown error'))
    } finally {
      setTtsBusy(false)
    }
  }

  const [imageBusy, setImageBusy] = useState(false)

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    setImageBusy(true)
    try {
      const formData = new FormData()
      formData.append('file', file)

      const { data } = await api.post<{ url: string }>('/api/upload/image', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })

      if (data?.url) {
        setForm((prev) => ({ ...prev, imageUrl: data.url }))
      }
    } catch (err) {
      console.error('Image upload error:', err)
      alert('Không upload được ảnh. Vui lòng thử lại.')
    } finally {
      setImageBusy(false)
    }
  }

  if (!isNew && poiQ.isLoading) return <p>Đang tải...</p>

  const isFormValid = form.name.trim().length > 0 && (form.ownerInfo?.trim().length ?? 0) > 0 &&
    (role !== 'Admin' || (!!form.ownerUserId && (ownersQ.data?.length ?? 0) > 0))
  const currentLanguage = ttsLanguages.find((item) => item.code === ttsLangCode) ?? ttsLanguages[0]
  const currentAudioUrl =
    ttsLangCode === 'vi' ? form.audioViUrl : (findTranslation(form, ttsLangCode)?.audioUrl ?? null)
  const currentTranslatedText =
    ttsLangCode === 'vi' ? form.description : (findTranslation(form, ttsLangCode)?.description ?? '')

  return (
    <div className="mx-auto max-w-2xl space-y-4">
      <h2 className="text-lg font-semibold">{isNew ? 'POI mới' : `Sửa POI #${id}`}</h2>

      <div className="rounded-lg border border-orange-200 bg-orange-50 p-3 text-sm text-orange-800">
        Form này chỉ cần nhập <strong>địa chỉ / số nhà</strong>. Hệ thống sẽ tự đổi địa chỉ thành tọa độ để
        POI vẫn hiển thị đúng trên bản đồ và dùng được cho GPS/geofence.
      </div>

      <div className="grid gap-4">
        <label className="text-sm">
          Tên
          <input
            className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
          />
        </label>

        {isAdmin && (
          <div className="flex flex-col gap-1">
            <label className="text-sm">
              Chủ sở hữu (Dành cho Admin)
              <select
                className="mt-1 w-full rounded border px-2 py-2 dark:border-stone-600 dark:bg-stone-800"
                value={form.ownerUserId ?? ''}
                onChange={(e) => setForm({ ...form, ownerUserId: e.target.value ? parseInt(e.target.value) : null })}
              >
                <option value="">-- Không có chủ sở hữu --</option>
                {owners.map(o => (
                  <option key={o.id} value={o.id}>
                    [{o.displayId || '---'}] {o.fullName || o.username} ({o.username})
                  </option>
                ))}
              </select>
            </label>
            {ownerMessage && <p className="text-xs text-red-500">{ownerMessage}</p>}
          </div>
        )}

        <label className="text-sm">
          Mô tả
          <textarea
            className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
            rows={3}
            value={form.description}
            onChange={(e) => {
              setForm({ ...form, description: e.target.value })
              setTtsMessage('')
            }}
          />
        </label>

        <div className="rounded-lg border border-stone-200 p-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h3 className="text-sm font-semibold text-stone-800">Voice từ mô tả</h3>
              <p className="mt-1 text-xs text-stone-500">
                Chọn ngôn ngữ cần xuất. Bạn có thể <strong>Nghe thử</strong> trước, sau đó mới bấm <strong>Xuất mp3</strong>.
              </p>
            </div>
            <div className="flex flex-col gap-2 sm:flex-row">
              <select
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-700"
                value={ttsLangCode}
                onChange={(e) => setTtsLangCode(e.target.value)}
                disabled={previewBusy || ttsBusy || save.isPending || audioBusy}
              >
                {ttsLanguages.map((item) => (
                  <option key={item.code} value={item.code}>
                    {item.label}
                  </option>
                ))}
              </select>
              <button
                type="button"
                className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 disabled:opacity-50"
                onClick={async () => {
                  try {
                    await previewVoiceFromDescription()
                  } catch {
                    // Message already shown.
                  }
                }}
                disabled={previewBusy || ttsBusy || save.isPending || audioBusy}
              >
                {previewBusy ? 'Đang nghe thử...' : 'Nghe thử'}
              </button>
              <button
                type="button"
                className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 disabled:opacity-50"
                onClick={async () => {
                  try {
                    await generateVoiceFromDescription()
                  } catch {
                    // Message already shown.
                  }
                }}
                disabled={previewBusy || ttsBusy || save.isPending || audioBusy}
              >
                {ttsBusy ? 'Đang xuất mp3...' : 'Xuất mp3'}
              </button>
            </div>
          </div>

          <div className="mt-3 flex flex-col gap-2 sm:flex-row sm:items-center">
            <button
              type="button"
              className="flex-1 rounded-lg border border-blue-500 bg-blue-50 px-4 py-2 text-sm font-medium text-blue-700 hover:bg-blue-100 disabled:opacity-50"
              onClick={async () => {
                generateVoiceForAllLangs()
              }}
              disabled={previewBusy || ttsBusy || save.isPending || audioBusy}
            >
              ⚡ Dịch & Trích xuất MP3 Tự động All Language
            </button>
          </div>

          <div className="mt-3 rounded-md bg-stone-50 p-3 text-sm text-stone-700">
            {ttsMessage || 'Chưa nghe thử hoặc xuất file mp3 từ mô tả.'}
          </div>

          {ttsLangCode !== 'vi' && currentTranslatedText && (
            <div className="mt-3 rounded-md border border-stone-200 bg-white p-3 text-sm text-stone-700">
              <p className="font-medium text-stone-800">Nội dung hiện dùng cho {currentLanguage.label}</p>
              <p className="mt-2 whitespace-pre-wrap">{currentTranslatedText}</p>
            </div>
          )}
        </div>

        <label className="text-sm">
          Địa chỉ / số nhà
          <input
            className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
            placeholder="Ví dụ: 123 Vĩnh Khánh, Phường Khánh Hội, Quận 4"
            value={form.ownerInfo ?? ''}
            onChange={(e) => {
              setForm({ ...form, ownerInfo: e.target.value })
              setGeoMessage('')
            }}
          />
          <div className="mt-1 text-xs text-stone-500">
            Nên nhập càng rõ càng tốt để hệ thống tìm đúng vị trí trên bản đồ.
          </div>
        </label>

        <div className="rounded-lg border border-stone-200 p-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h3 className="text-sm font-semibold text-stone-800">Định vị từ địa chỉ</h3>
              <p className="mt-1 text-xs text-stone-500">
                Bạn không cần nhập lat/lon. Bấm nút dưới đây để hệ thống tự lấy tọa độ.
              </p>
            </div>
            <button
              type="button"
              className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 disabled:opacity-50"
              onClick={async () => {
                try {
                  await resolveAddressToCoordinates()
                } catch (err) {
                  setGeoMessage((err as Error).message)
                }
              }}
              disabled={geoBusy || save.isPending}
            >
              {geoBusy ? 'Đang định vị...' : 'Lấy tọa độ từ địa chỉ'}
            </button>
          </div>

          <div className="mt-3 rounded-md bg-stone-50 p-3 text-sm text-stone-700">
            {geoMessage || 'Chưa định vị địa chỉ.'}
          </div>

          <div className="mt-2 text-xs text-stone-500">
            Tọa độ đang lưu ngầm: {form.latitude.toFixed(6)}, {form.longitude.toFixed(6)}
          </div>
        </div>

        <label className="text-sm">
          Mã QR (bus / điểm dừng, ví dụ <span className="font-mono">VK-POI-001</span>)
          <div className="mt-1 text-xs text-stone-500">
            Hệ thống sẽ tự động sinh mã dạng <strong>VK-POI-[ID]</strong> nếu bạn để trống.
          </div>
          <input
            className="mt-1 w-full rounded border px-2 py-1 font-mono dark:border-stone-600 dark:bg-stone-800"
            placeholder="Để trống để tự động sinh mã"
            value={form.qrCode ?? ''}
            onChange={(e) => setForm({ ...form, qrCode: e.target.value.trim() || null })}
          />
          {!isNew && form.qrCode && (
            <div className="mt-3 flex flex-col items-start gap-3">
              <img
                src={`/api/poi/${id}/qrcode`}
                alt={`QR code cho ${form.name}`}
                className="h-40 w-40 rounded border border-stone-200 bg-white p-1"
                onError={(e) => { (e.target as HTMLImageElement).style.display = 'none' }}
              />
              <a
                href={`/api/poi/${id}/qrcode`}
                download={`QR-${form.qrCode ?? id}.png`}
                className="rounded-lg border border-stone-300 px-4 py-2 text-sm font-medium text-stone-700 hover:bg-stone-50"
              >
                Tải ảnh QR về in
              </a>
            </div>
          )}
          {isNew && (
            <p className="mt-2 text-xs text-stone-400">Lưu POI trước, sau đó quay lại để xem ảnh QR.</p>
          )}
        </label>

      {!isNew && form.qrCode && (
        <div className="rounded-lg border border-stone-200 p-4">
          <h3 className="text-sm font-semibold text-stone-800">Hình ảnh Mã QR</h3>
          <div className="mt-3 flex items-start gap-4">
            <div className="h-32 w-32 items-center justify-center overflow-hidden rounded border bg-white">
              <img
                src={`/api/poi/${id}/qrcode?t=${new Date().getTime()}`}
                alt="POI QR Code"
                className="h-full w-full object-contain"
              />
            </div>
            <div className="flex-1">
              <p className="text-xs text-stone-500">
                Mã định danh: <span className="font-mono font-bold text-blue-600">{form.qrCode}</span>
              </p>
              <button
                type="button"
                className="mt-3 rounded-lg bg-blue-100 px-4 py-2 text-sm font-medium text-blue-700 hover:bg-blue-200"
                onClick={() => {
                  window.open(`/api/poi/${id}/qrcode`, '_blank')
                }}
              >
                📥 Tải mã QR (Mở Tab mới để in)
              </button>
            </div>
          </div>
        </div>
      )}

      <div className="rounded-lg border border-stone-200 p-4">
        <h3 className="text-sm font-semibold text-stone-800">Hiển thị trên bản đồ nội bộ</h3>
        <div className="mt-3 grid grid-cols-2 gap-2">
          <label className="text-sm">
            Map X %
            <input
              type="number"
              className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
              value={form.mapX}
              onChange={(e) => setForm({ ...form, mapX: Number(e.target.value) })}
            />
          </label>
          <label className="text-sm">
            Map Y %
            <input
              type="number"
              className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
              value={form.mapY}
              onChange={(e) => setForm({ ...form, mapY: Number(e.target.value) })}
            />
          </label>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-2">
        <label className="text-sm">
          Bán kính kích hoạt (m)
          <input
            type="number"
            className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
            value={form.triggerRadiusMeters}
            onChange={(e) => setForm({ ...form, triggerRadiusMeters: Number(e.target.value) })}
          />
        </label>
        <label className="text-sm">
          Ưu tiên
          <input
            type="number"
            className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
            value={form.priority}
            onChange={(e) => setForm({ ...form, priority: Number(e.target.value) })}
          />
        </label>
      </div>

      <label className="text-sm">
        Cooldown (giây)
        <input
          type="number"
          className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
          value={form.cooldownSeconds}
          onChange={(e) => setForm({ ...form, cooldownSeconds: Number(e.target.value) })}
        />
      </label>

      <div className="flex flex-col gap-2">
        <label className="text-sm font-semibold">Hình ảnh Quán ăn</label>
        <div className="flex items-center gap-4">
          {form.imageUrl && (
            <div className="relative h-20 w-20 overflow-hidden rounded-lg border">
              <img src={form.imageUrl} className="h-full w-full object-cover" />
            </div>
          )}
          <div className="flex-1">
            <input
              type="file"
              accept="image/*"
              onChange={handleImageUpload}
              className="hidden"
              id="image-upload"
            />
            <label
              htmlFor="image-upload"
              className="inline-block cursor-pointer rounded-lg bg-orange-100 px-4 py-2 text-sm font-medium text-orange-700 hover:bg-orange-200"
            >
              {imageBusy ? 'Đang tải lên...' : 'Chọn ảnh từ máy'}
            </label>
            <p className="mt-1 text-xs text-stone-500">Hỗ trợ: JPG, PNG, WEBP. Tối đa 10MB.</p>
          </div>
        </div>
      </div>

      <label className="text-sm">
        Audio VI URL
        <input
          className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
          value={form.audioViUrl ?? ''}
          onChange={(e) => setForm({ ...form, audioViUrl: e.target.value || null })}
        />
      </label>

      {currentAudioUrl && (
        <div className="rounded-lg border border-stone-200 p-4">
          <p className="text-sm font-semibold text-stone-800">
            Nghe thử audio hiện tại ({currentLanguage.label})
          </p>
          <audio className="mt-3 w-full" controls src={currentAudioUrl} />
          <button
            type="button"
            className="mt-3 rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 disabled:opacity-50"
            onClick={async () => {
              try {
                setAudioBusy(true)
                const audio = new Audio(currentAudioUrl ?? '')
                await audio.play()
              } finally {
                setAudioBusy(false)
              }
            }}
            disabled={audioBusy}
          >
            {audioBusy ? 'Đang phát...' : 'Nghe file mp3 đã xuất'}
          </button>
        </div>
      )}

      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={form.isActive}
          onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
        />
        Đang hoạt động
      </label>

      {!isNew && form.contentVersion != null && (
        <p className="text-xs text-stone-500">
          Phiên bản nội dung (đồng bộ app): <span className="font-mono">{form.contentVersion}</span>
        </p>
      )}
    </div>

      {
    save.error && (
      <div className="rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-700 dark:border-red-700 dark:bg-red-900/20">
        <strong>Lỗi:</strong> {(save.error as Error).message}
      </div>
    )
  }

  {
    !isFormValid && (
      <div className="rounded-lg border border-yellow-300 bg-yellow-50 p-3 text-sm text-yellow-700 dark:border-yellow-700 dark:bg-yellow-900/20">
        Vui lòng điền <strong>Tên</strong> và <strong>Địa chỉ</strong> POI.
      </div>
    )
  }

  { save.isPending && <p className="text-sm text-blue-600">Đang lưu...</p> }

  <div className="flex gap-2">
    <button
      type="button"
      className="rounded-lg bg-orange-600 px-4 py-2 text-white disabled:opacity-50"
      onClick={() => save.mutate()}
      disabled={save.isPending || geoBusy || (!isNew && !id) || !isFormValid}
      title={!isFormValid ? 'Điền tên và địa chỉ POI trước' : ''}
    >
      Lưu
    </button>
    <button type="button" className="rounded-lg border px-4 py-2" onClick={() => navigate(-1)}>
      Hủy
    </button>
    {!isNew && role === 'Admin' && (
      <button
        type="button"
        className="ml-auto rounded-lg border border-red-300 px-4 py-2 text-red-700"
        onClick={async () => {
          if (!confirm('Vô hiệu hóa POI này?')) return
          await api.delete(`/api/poi/${id}`)
          qc.invalidateQueries({ queryKey: ['pois'] })
          navigate('/pois')
        }}
      >
        Vô hiệu hóa
      </button>
    )}
  </div>
    </div >
  )
}