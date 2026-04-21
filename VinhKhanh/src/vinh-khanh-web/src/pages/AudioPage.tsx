import { useState, useEffect } from 'react'
import { api, type Poi } from '@/lib/api'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Search, Music, Play, Pause, Upload, Loader2, User } from 'lucide-react'
import { useAuthStore } from '@/store/authStore'


export function AudioPage() {
  const { role } = useAuthStore()
  const isAdmin = role === 'Admin'
  const qc = useQueryClient()

  const [searchTerm, setSearchTerm] = useState('')
  const [ownerFilter, setOwnerFilter] = useState<number | 'all'>('all')
  const [owners, setOwners] = useState<any[]>([])
  const [playingUrl, setPlayingUrl] = useState<string | null>(null)
  const [audioObj, setAudioObj] = useState<HTMLAudioElement | null>(null)

  useEffect(() => {
    if (isAdmin) {
      api.get('/api/auth/owners')
        .then(res => setOwners(res.data))
        .catch(err => console.error('Lỗi tải owner:', err))
    }
    
    // Dọn dẹp audio khi rời trang
    return () => {
      audioObj?.pause()
    }
  }, [isAdmin, audioObj])

  const [uploadingPoiId, setUploadingPoiId] = useState<number | null>(null)
  const [uploadingLang, setUploadingLang] = useState<string | null>(null)

  const { data: pois = [], isLoading } = useQuery({
    queryKey: ['pois-audio'],
    queryFn: async () => (await api.get<Poi[]>('/api/poi')).data,
  })

  // Lọc quán ăn
  const filteredPois = pois.filter(p => {
    const matchName = p.name.toLowerCase().includes(searchTerm.toLowerCase())
    const matchOwner = ownerFilter === 'all' || p.ownerUserId === ownerFilter
    return matchName && matchOwner
  })

  const togglePlay = (url: string) => {
    if (playingUrl === url) {
      audioObj?.pause()
      setPlayingUrl(null)
      setAudioObj(null)
    } else {
      audioObj?.pause()
      const newAudio = new Audio(url)
      newAudio.play()
      newAudio.onended = () => setPlayingUrl(null)
      setPlayingUrl(url)
      setAudioObj(newAudio)
    }
  }

  async function handleUpload(e: React.ChangeEvent<HTMLInputElement>, poiId: number, lang: string) {
    const file = e.target.files?.[0]
    if (!file) return

    setUploadingPoiId(poiId)
    setUploadingLang(lang)

    try {
      const fd = new FormData()
      fd.append('file', file)
      fd.append('lang', lang)

      // Upload file
      const { data } = await api.post('/api/audio/upload', fd)
      const audioUrl = data.url ?? data.filename

      // Gán URL vào POI thông qua API (sử dụng endpoint /api/poi/{id} hoặc tạo endpoint riêng)
      // Để đơn giản và nhanh, ta lợi dụng endpoint update POI hiện có
      const targetPoi = pois.find(p => p.id === poiId)
      if (!targetPoi) return

      if (lang === 'vi') {
        await api.put(`/api/poi/${poiId}`, { ...targetPoi, audioViUrl: audioUrl })
      } else {
        const trans = targetPoi.translations?.find(t => t.languageCode === lang)
        const updatedTranslations = trans
          ? targetPoi.translations?.map(t => t.languageCode === lang ? { ...t, audioUrl } : t)
          : [...(targetPoi.translations ?? []), { languageCode: lang, name: targetPoi.name, description: targetPoi.description, audioUrl }]

        await api.put(`/api/poi/${poiId}`, { ...targetPoi, translations: updatedTranslations })
      }

      qc.invalidateQueries({ queryKey: ['pois-audio'] })
      alert(`Đã cập nhật audio ${lang} cho quán ${targetPoi.name}`)
    } catch (err) {
      console.error(err)
      alert('Lỗi khi cập nhật audio.')
    } finally {
      setUploadingPoiId(null)
      setUploadingLang(null)
      e.target.value = ''
    }
  }

  const langs = [
    { code: 'vi', label: 'VI' },
    { code: 'en', label: 'EN' },
    { code: 'ja', label: 'JA' },
    { code: 'ko', label: 'KO' },
    { code: 'zh', label: 'ZH' },
  ]

  if (isLoading) return (
    <div className="flex flex-col items-center justify-center py-20 gap-4">
      <Loader2 className="animate-spin text-orange-600" size={48} />
      <p className="text-slate-500 font-medium">Đang tải danh sách âm thanh...</p>
    </div>
  )

  return (
    <div className="space-y-6">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Quản lý Audio</h1>
          <p className="text-sm text-slate-500">Quản lý tệp âm thanh thuyết minh đa ngôn ngữ cho từng quán ăn.</p>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
          <input
            type="text"
            placeholder="Tìm theo tên quán ăn..."
            className="w-full pl-10 pr-4 py-2 rounded-lg border border-slate-200 focus:outline-none focus:ring-2 focus:ring-orange-500 transition-all shadow-sm"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>
        {isAdmin && (
          <div className="relative">
            <User className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
            <select
              className="w-full pl-10 pr-4 py-2 rounded-lg border border-slate-200 focus:outline-none focus:ring-2 focus:ring-orange-500 transition-all shadow-sm appearance-none bg-white"
              value={ownerFilter}
              onChange={(e) => setOwnerFilter(e.target.value === 'all' ? 'all' : parseInt(e.target.value))}
            >
              <option value="all">Tất cả chủ quán</option>
              {owners.map(o => (
                <option key={o.id} value={o.id}>
                  [{o.displayId || '---'}] {o.fullName || o.username}
                </option>
              ))}
            </select>
          </div>
        )}
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead className="bg-slate-50 border-b border-slate-200">
              <tr>
                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Quán Ăn</th>
                {isAdmin && <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Chủ Quán</th>}
                {langs.map(l => (
                  <th key={l.code} className="px-4 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider text-center">{l.label}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {filteredPois.map((p) => (
                <tr key={p.id} className="hover:bg-slate-50 transition-colors">
                  <td className="px-6 py-4">
                    <div className="font-bold text-slate-800">{p.name}</div>
                    <div className="text-xs text-slate-500 truncate max-w-[200px]">{p.ownerInfo}</div>
                  </td>
                  {isAdmin && (
                    <td className="px-6 py-4">
                      <div className="text-sm font-bold text-orange-700 font-mono italic">
                        {p.owner?.displayId || '---'}
                      </div>
                    </td>
                  )}
                  {langs.map(l => {
                    const url = l.code === 'vi'
                      ? p.audioViUrl
                      : p.translations?.find(t => t.languageCode === l.code)?.audioUrl

                    const isUploading = uploadingPoiId === p.id && uploadingLang === l.code

                    return (
                      <td key={l.code} className="px-4 py-4 text-center">
                        <div className="flex flex-col items-center gap-2">
                          {url ? (
                            <button
                              onClick={() => togglePlay(url)}
                              className={`p-2 rounded-full transition-all ${playingUrl === url ? 'bg-orange-600 text-white shadow-md scale-110' : 'bg-orange-100 text-orange-600 hover:bg-orange-200'
                                }`}
                              title="Nghe thử"
                            >
                              {playingUrl === url ? <Pause size={16} /> : <Play size={16} />}
                            </button>
                          ) : (
                            <div className="w-8 h-8 rounded-full bg-slate-100 flex items-center justify-center text-slate-300" title="Chưa có audio">
                              <Music size={14} />
                            </div>
                          )}

                          <label className={`cursor-pointer p-1.5 rounded-md hover:bg-slate-100 text-slate-400 hover:text-orange-600 transition-colors ${isUploading ? 'animate-pulse' : ''}`}>
                            <input
                              type="file"
                              className="hidden"
                              accept="audio/*"
                              onChange={(e) => handleUpload(e, p.id!, l.code)}
                            />
                            {isUploading ? <Loader2 className="animate-spin" size={14} /> : <Upload size={14} />}
                          </label>
                        </div>
                      </td>
                    )
                  })}
                </tr>
              ))}
              {filteredPois.length === 0 && (
                <tr>
                  <td colSpan={isAdmin ? 7 : 6} className="px-6 py-10 text-center text-slate-500">
                    Không tìm thấy quán ăn nào phù hợp.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
