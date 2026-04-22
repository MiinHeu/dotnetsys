import { useEffect, useState } from 'react';
import { Smartphone, Download, QrCode, AlertTriangle, Apple } from 'lucide-react';
import qrImage from '../assets/qr-download.png';

export function DownloadPage() {
  const downloadUrl = 'https://drive.google.com/uc?export=download&id=1PXrLjhoT7zWacxQVf7jJxCrRBj8Yl-Qk';
  const [os, setOs] = useState<'android' | 'ios' | 'other'>('other');

  useEffect(() => {
    const ua = navigator.userAgent.toLowerCase();
    if (ua.includes('android')) setOs('android');
    else if (ua.includes('iphone') || ua.includes('ipad') || ua.includes('ipod')) setOs('ios');
  }, []);

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
            <h2 className="text-sm font-bold text-stone-400 uppercase tracking-widest">
              {os === 'ios' ? 'Hướng dẫn cho iPhone (iOS)' : 'Hướng dẫn cho Android'}
            </h2>
            <ul className="space-y-3">
              {os === 'ios' ? (
                <>
                  <li className="flex gap-3 text-stone-700 italic">
                    <span className="flex-shrink-0 w-6 h-6 rounded-full bg-blue-100 text-blue-500 flex items-center justify-center text-xs font-bold font-mono">01</span>
                    Cài đặt ứng dụng <strong>TestFlight</strong> từ App Store.
                  </li>
                  <li className="flex gap-3 text-stone-700 italic">
                    <span className="flex-shrink-0 w-6 h-6 rounded-full bg-blue-100 text-blue-500 flex items-center justify-center text-xs font-bold font-mono">02</span>
                    Quét mã QR hoặc nhấn vào nút bên dưới để nhận lời mời trải nghiệm.
                  </li>
                  <li className="flex gap-3 text-stone-700 italic">
                    <span className="flex-shrink-0 w-6 h-6 rounded-full bg-blue-100 text-blue-500 flex items-center justify-center text-xs font-bold font-mono">03</span>
                    Chấp nhận lời mời và bắt đầu khám phá Vĩnh Khánh.
                  </li>
                </>
              ) : (
                <>
                  <li className="flex gap-3 text-stone-700 italic">
                    <span className="flex-shrink-0 w-6 h-6 rounded-full bg-stone-100 text-stone-500 flex items-center justify-center text-xs font-bold font-mono">01</span>
                    Quét mã QR hoặc nhấn "Tải APK" để tải file cài đặt.
                  </li>
                  <li className="flex gap-3 text-stone-700 italic">
                    <span className="flex-shrink-0 w-6 h-6 rounded-full bg-stone-100 text-stone-500 flex items-center justify-center text-xs font-bold font-mono">02</span>
                    Mở file đã tải và chọn "Cài đặt" (Cho phép nguồn không xác định nếu có).
                  </li>
                  <li className="flex gap-3 text-stone-700 italic">
                    <span className="flex-shrink-0 w-6 h-6 rounded-full bg-stone-100 text-stone-500 flex items-center justify-center text-xs font-bold font-mono">03</span>
                    Bật GPS và bắt đầu hành trình ẩm thực.
                  </li>
                </>
              )}
            </ul>
          </div>

          <div className="pt-4 flex flex-wrap gap-4">
            {os === 'ios' ? (
              <button className="inline-flex items-center gap-3 px-8 py-4 bg-stone-400 text-white rounded-2xl font-bold cursor-not-allowed">
                <Apple size={20} />
                Sắp có trên App Store
              </button>
            ) : (
              <a 
                href={downloadUrl}
                className="inline-flex items-center gap-3 px-8 py-4 bg-stone-900 text-white rounded-2xl font-bold hover:bg-stone-800 transition-all hover:translate-y-[-2px] active:translate-y-0 shadow-lg shadow-stone-200"
              >
                <Download size={20} />
                Tải APK trực tiếp
              </a>
            )}
          </div>
        </div>
      </div>

      {/* Info Box */}
      <div className="bg-blue-50 border border-blue-200 p-6 rounded-2xl flex gap-4 items-start">
        <AlertTriangle className="text-blue-600 flex-shrink-0" size={24} />
        <div className="space-y-1">
          <h4 className="font-bold text-blue-900">Thông tin hỗ trợ</h4>
          <p className="text-sm text-blue-800 leading-relaxed">
            {os === 'ios' 
              ? 'Vì chính sách bảo mật của Apple, ứng dụng iPhone hiện chỉ được phân phối qua TestFlight cho các thiết bị đăng ký trước.' 
              : 'Ứng dụng Android có thể cài đặt trực tiếp. Nếu bạn gặp lỗi "Ứng dụng chưa được ký", hãy yên tâm vì đây là bản thử nghiệm nội bộ.'}
          </p>
        </div>
      </div>
    </div>
  );
}
