import { useState } from 'react'
import { useNavigate } from 'react-router'
import { api } from '@/lib/api'
import { useAuthStore } from '@/store/authStore'
import { Eye, EyeOff, LogIn, AlertCircle } from 'lucide-react'

export function Login() {
  const [u, setU] = useState('')
  const [p, setP] = useState('')
  const [err, setErr] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const setAuth = useAuthStore((s) => s.setAuth)
  const navigate = useNavigate()

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setErr(null)
    setLoading(true)
    try {
      const { data } = await api.post('/api/auth/login', {
        username: u,
        password: p,
      })
      setAuth(data.token, data.role)
      navigate('/', { replace: true })
    } catch {
      setErr('Sai tài khoản hoặc mật khẩu. Vui lòng thử lại.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex" style={{ fontFamily: "'Nunito', sans-serif", background: 'linear-gradient(135deg, #FFFBF5 0%, #FFF3E6 50%, #F5E6D0 100%)' }}>
      {/* Left brand panel */}
      <div className="hidden lg:flex lg:w-1/2 relative overflow-hidden" style={{ background: 'linear-gradient(135deg, #6B4226 0%, #8B5E3C 40%, #A0704D 100%)' }}>
        {/* Animated floating elements */}
        <div className="absolute inset-0">
          <div className="absolute top-16 left-12 w-72 h-72 rounded-full opacity-8" style={{ background: 'rgba(212,114,46,0.15)', animation: 'vkFloat 7s ease-in-out infinite' }} />
          <div className="absolute bottom-24 right-10 w-56 h-56 rounded-full opacity-8" style={{ background: 'rgba(212,168,71,0.12)', animation: 'vkFloat 9s ease-in-out infinite 1.5s' }} />
          <div className="absolute top-1/2 left-1/4 w-40 h-40 rounded-full opacity-8" style={{ background: 'rgba(201,120,120,0.1)', animation: 'vkFloat 8s ease-in-out infinite 3s' }} />
        </div>

        {/* Food icons floating */}
        <div className="absolute inset-0 pointer-events-none">
          <span className="absolute top-[15%] right-[20%] text-5xl opacity-20" style={{ animation: 'vkBob 5s ease-in-out infinite' }}>🍜</span>
          <span className="absolute top-[45%] left-[15%] text-4xl opacity-15" style={{ animation: 'vkBob 6s ease-in-out infinite 1s' }}>🥖</span>
          <span className="absolute bottom-[20%] right-[30%] text-5xl opacity-20" style={{ animation: 'vkBob 7s ease-in-out infinite 2s' }}>🍲</span>
          <span className="absolute top-[70%] left-[40%] text-3xl opacity-15" style={{ animation: 'vkBob 5.5s ease-in-out infinite 0.5s' }}>🥢</span>
          <span className="absolute top-[30%] left-[60%] text-4xl opacity-15" style={{ animation: 'vkBob 6.5s ease-in-out infinite 1.5s' }}>🧆</span>
        </div>

        {/* Content */}
        <div className="relative z-10 flex flex-col justify-center px-16 text-white">
          <div className="flex items-center gap-4 mb-10">
            <div className="w-16 h-16 rounded-2xl flex items-center justify-center text-4xl shadow-xl" style={{ background: 'rgba(255,255,255,0.15)', backdropFilter: 'blur(10px)' }}>
              🍜
            </div>
            <div>
              <h1 style={{ color: '#FFF', margin: 0, fontSize: '30px', fontWeight: 800, letterSpacing: '-0.5px', fontFamily: "'Nunito', sans-serif" }}>
                Vĩnh Khánh
              </h1>
              <p style={{ color: '#E8C96A', fontSize: '14px', fontWeight: 600, margin: '2px 0 0 0' }}>Phố Ẩm Thực • Quận 4</p>
            </div>
          </div>

          <h2 style={{ color: '#FFF', fontSize: '42px', lineHeight: '1.15', fontWeight: 800, letterSpacing: '-1px', margin: '0 0 24px 0', fontFamily: "'Nunito', sans-serif" }}>
            Khám Phá<br />
            Hương Vị<br />
            <span style={{ color: '#E8C96A' }}>Sài Gòn</span>
          </h2>

          <p style={{ color: '#D4B896', fontSize: '17px', lineHeight: '1.7', maxWidth: '420px', margin: '0 0 40px 0' }}>
            Hệ thống thuyết minh tự động đa ngôn ngữ — giúp du khách trải nghiệm 
            phố ẩm thực Vĩnh Khánh một cách trọn vẹn nhất.
          </p>

          {/* Feature highlights — user-friendly language */}
          <div className="space-y-5">
            {[
              { emoji: '📍', label: 'Tự động giới thiệu khi bạn đến gần quán' },
              { emoji: '🌏', label: 'Hỗ trợ 5 ngôn ngữ: Việt, Anh, Hoa, Hàn, Nhật' },
              { emoji: '🗺️', label: 'Lộ trình khám phá ẩm thực được gợi ý sẵn' },
            ].map(({ emoji, label }) => (
              <div key={label} className="flex items-center gap-4">
                <div className="w-11 h-11 rounded-xl flex items-center justify-center text-xl" style={{ background: 'rgba(255,255,255,0.12)', backdropFilter: 'blur(8px)' }}>
                  {emoji}
                </div>
                <span style={{ color: '#E8D5BD', fontSize: '15px', fontWeight: 500 }}>{label}</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Right login panel */}
      <div className="flex-1 flex items-center justify-center p-6 sm:p-10">
        <div className="w-full max-w-md">
          {/* Mobile logo */}
          <div className="lg:hidden text-center mb-8">
            <div className="inline-flex items-center gap-3">
              <div className="w-14 h-14 rounded-xl flex items-center justify-center text-3xl shadow-lg" style={{ background: 'linear-gradient(135deg, #6B4226, #8B5E3C)' }}>
                🍜
              </div>
              <div className="text-left">
                <h1 style={{ color: '#6B4226', margin: 0, fontSize: '24px', fontWeight: 800, fontFamily: "'Nunito', sans-serif" }}>Vĩnh Khánh</h1>
                <p style={{ color: '#A0704D', fontSize: '13px', fontWeight: 600 }}>Phố Ẩm Thực • Quận 4</p>
              </div>
            </div>
          </div>

          {/* Login card */}
          <div className="rounded-2xl shadow-2xl p-8 sm:p-10" style={{ background: '#FFFFFF', border: '1px solid #E8D5BD' }}>
            <div className="text-center mb-8">
              <h2 style={{ color: '#6B4226', margin: '0 0 8px 0', fontSize: '28px', fontWeight: 800, letterSpacing: '-0.5px', fontFamily: "'Nunito', sans-serif" }}>
                Chào Mừng Bạn
              </h2>
              <p style={{ color: '#8B7B6B', fontSize: '15px' }}>
                Đăng nhập để quản lý hệ thống
              </p>
            </div>

            {/* Error */}
            {err && (
              <div className="rounded-xl p-4 mb-6 flex items-start gap-3" style={{ background: '#FEF2F2', border: '1px solid #FECACA' }}>
                <AlertCircle className="h-5 w-5 flex-shrink-0 mt-0.5" style={{ color: '#DC2626' }} />
                <span style={{ color: '#DC2626', fontSize: '14px', fontWeight: 600 }}>{err}</span>
              </div>
            )}

            <form className="space-y-5" onSubmit={onSubmit}>
              {/* Username */}
              <div>
                <label htmlFor="login-username" className="block mb-2" style={{ color: '#6B4226', fontSize: '14px', fontWeight: 700 }}>
                  Tài khoản
                </label>
                <input
                  id="login-username"
                  className="block w-full px-4 py-3.5 rounded-xl text-base outline-none transition-all duration-200"
                  style={{ background: '#FFFBF5', border: '2px solid #E8D5BD', color: '#4A3728', fontFamily: "'Nunito', sans-serif" }}
                  value={u}
                  onChange={(e) => setU(e.target.value)}
                  autoComplete="username"
                  placeholder="Nhập tên đăng nhập"
                  onFocus={(e) => { e.target.style.borderColor = '#D4722E'; e.target.style.boxShadow = '0 0 0 3px rgba(212,114,46,0.12)' }}
                  onBlur={(e) => { e.target.style.borderColor = '#E8D5BD'; e.target.style.boxShadow = 'none' }}
                />
              </div>

              {/* Password */}
              <div>
                <label htmlFor="login-password" className="block mb-2" style={{ color: '#6B4226', fontSize: '14px', fontWeight: 700 }}>
                  Mật khẩu
                </label>
                <div className="relative">
                  <input
                    id="login-password"
                    type={showPassword ? "text" : "password"}
                    className="block w-full px-4 py-3.5 pr-12 rounded-xl text-base outline-none transition-all duration-200"
                    style={{ background: '#FFFBF5', border: '2px solid #E8D5BD', color: '#4A3728', fontFamily: "'Nunito', sans-serif" }}
                    value={p}
                    onChange={(e) => setP(e.target.value)}
                    autoComplete="current-password"
                    placeholder="Nhập mật khẩu"
                    onFocus={(e) => { e.target.style.borderColor = '#D4722E'; e.target.style.boxShadow = '0 0 0 3px rgba(212,114,46,0.12)' }}
                    onBlur={(e) => { e.target.style.borderColor = '#E8D5BD'; e.target.style.boxShadow = 'none' }}
                  />
                  <button
                    type="button"
                    className="absolute inset-y-0 right-0 pr-4 flex items-center"
                    onClick={() => setShowPassword(!showPassword)}
                  >
                    {showPassword ? (
                      <EyeOff className="h-5 w-5" style={{ color: '#A89888' }} />
                    ) : (
                      <Eye className="h-5 w-5" style={{ color: '#A89888' }} />
                    )}
                  </button>
                </div>
              </div>

              {/* Submit */}
              <button
                type="submit"
                disabled={loading || !u || !p}
                className="group relative w-full flex justify-center items-center py-3.5 px-4 text-base rounded-xl text-white transition-all duration-300 disabled:opacity-50 disabled:cursor-not-allowed"
                style={{
                  background: 'linear-gradient(135deg, #6B4226 0%, #8B5E3C 100%)',
                  boxShadow: '0 4px 14px rgba(107,66,38,0.35)',
                  fontWeight: 700,
                  fontFamily: "'Nunito', sans-serif",
                  fontSize: '16px',
                }}
                onMouseEnter={(e) => { if (!loading) { e.currentTarget.style.boxShadow = '0 6px 20px rgba(107,66,38,0.5)'; e.currentTarget.style.transform = 'translateY(-1px)' } }}
                onMouseLeave={(e) => { e.currentTarget.style.boxShadow = '0 4px 14px rgba(107,66,38,0.35)'; e.currentTarget.style.transform = 'translateY(0)' }}
              >
                {loading ? (
                  <svg className="animate-spin h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                  </svg>
                ) : (
                  <>
                    <LogIn className="h-5 w-5 mr-2 group-hover:translate-x-0.5 transition-transform duration-200" />
                    Đăng Nhập
                  </>
                )}
              </button>
            </form>
          </div>

          {/* Footer */}
          <div className="mt-8 text-center">
            <p style={{ color: '#A89888', fontSize: '12px' }}>
              © 2026 Phố Ẩm Thực Vĩnh Khánh — Quận 4, TP. Hồ Chí Minh
            </p>
          </div>
        </div>
      </div>

      {/* Animations */}
      <style>{`
        @keyframes vkFloat {
          0%, 100% { transform: translateY(0px) rotate(0deg); }
          50% { transform: translateY(-25px) rotate(3deg); }
        }
        @keyframes vkBob {
          0%, 100% { transform: translateY(0px) scale(1); }
          50% { transform: translateY(-12px) scale(1.05); }
        }
      `}</style>
    </div>
  )
}
