import { Smartphone, Download, QrCode, AlertTriangle } from 'lucide-react';
import qrImage from '../assets/qr-download.png';

export function DownloadPage() {
  const downloadUrl = 'https://drive.google.com/uc?export=download&id=1PXrLjhoT7zWacxQVf7jJxCrRBj8Yl-Qk';

  return (
    <div className="max-w-4xl mx-auto space-y-8 animate-in fade-in duration-700">
      <div className="flex flex-col md:flex-row items-center gap-12 bg-white p-10 rounded-3xl shadow-xl shadow-stone-200/50 border border-stone-100">
        
        {/* QR Section */}
        <div className="flex-shrink-0 space-y-4 text-center">
          <div className="relative p-6 bg-stone-50 rounded-2xl border-2 border-dashed border-stone-200 group transition-all hover:border-orange-200 hover:bg-orange-50/30">
            <img 
              src={qrImage} 
              alt="QR Download" 
              className="w-64 h-64 rounded-xl shadow-sm mix-blend-multiply group-hover:scale-105 transition-transform duration-500"
            />
            <div className="absolute -top-3 -right-3 bg-orange-600 text-white p-2 rounded-full shadow-lg">
              <QrCode size={20} />
            </div>
          </div>
          <p className="text-sm font-medium text-stone-500">Quét để tải ngay</p>
        </div>

        {/* Info Section */}
        <div className="flex-1 space-y-6">
          <header className="space-y-2">
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-orange-100 text-orange-700 text-xs font-bold uppercase tracking-wider">
              <Smartphone size={14} />
              Mobile Application
            </div>
            <h1 className="text-4xl font-extrabold text-stone-900 tracking-tight">Vinh Khanh Food Street</h1>
            <p className="text-lg text-stone-600 leading-relaxed">
              Trải nghiệm thực tế ảo tăng cường và bản đồ ẩm thực sống động ngay trên điện thoại của bạn.
            </p>
          </header>

          <div className="space-y-4">
            <h2 className="text-sm font-bold text-stone-400 uppercase tracking-widest">Hướng dẫn cài đặt</h2>
            <ul className="space-y-3">
              {[
                'Quét mã QR bên cạnh bằng điện thoại của bạn.',
                'Hệ thống sẽ dẫn bạn tới Google Drive để tải file.',
                'Chọn "Tải xuống" và bật "Cài đặt từ nguồn không xác định" nếu được hỏi.',
                'Mở file và bắt đầu trải nghiệm Vinh Khanh Food Street.'
              ].map((step, i) => (
                <li key={i} className="flex gap-3 text-stone-700 italic">
                  <span className="flex-shrink-0 w-6 h-6 rounded-full bg-stone-100 text-stone-500 flex items-center justify-center text-xs font-bold font-mono">
                    0{i + 1}
                  </span>
                  {step}
                </li>
              ))}
            </ul>
          </div>

          <div className="pt-4 flex flex-wrap gap-4">
            <a 
              href={downloadUrl}
              className="inline-flex items-center gap-3 px-8 py-4 bg-stone-900 text-white rounded-2xl font-bold hover:bg-stone-800 transition-all hover:translate-y-[-2px] active:translate-y-0 shadow-lg shadow-stone-200"
            >
              <Download size={20} />
              Tải APK trực tiếp
            </a>
          </div>
        </div>
      </div>

      {/* Info Box */}
      <div className="bg-blue-50 border border-blue-200 p-6 rounded-2xl flex gap-4 items-start">
        <AlertTriangle className="text-blue-600 flex-shrink-0" size={24} />
        <div className="space-y-1">
          <h4 className="font-bold text-blue-900">Liên kết Công cộng</h4>
          <p className="text-sm text-blue-800 leading-relaxed">
            Ứng dụng hiện đã được lưu trữ trên Google Drive. Bạn có thể quét mã này để tải app từ bất cứ đâu 
            (Sử dụng 4G hoặc WiFi bất kỳ) mà không cần phải ở gần máy chủ.
          </p>
        </div>
      </div>
    </div>
  );
}
