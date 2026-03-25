import { useEffect, useState } from 'react'
import reactLogo from './assets/react.svg'
import viteLogo from './assets/vite.svg'
import heroImg from './assets/hero.png'
import './App.css'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'

type Poi = {
  id: number
  name: string
}

function App() {
  const [count, setCount] = useState(0)
  const [pois, setPois] = useState<Poi[]>([])
  const [signalrConnected, setSignalrConnected] = useState(false)
  const [signalrError, setSignalrError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    fetch('/api/poi')
      .then((r) => r.json())
      .then((data) => {
        if (cancelled) return
        // API trả về có thể là array hoặc object có "value"
        const arr = (data?.value ?? data ?? []) as any[]
        setPois(
          arr.map((x) => ({
            id: x.id,
            name: x.name,
          })),
        )
      })
      .catch(() => {
        if (cancelled) return
        setPois([])
      })

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/vinh-khanh')
      .configureLogging(LogLevel.Information)
      .withAutomaticReconnect()
      .build()

    connection.on('PoiCreated', (poi: any) => {
      if (cancelled) return
      const next: Poi = { id: poi.id, name: poi.name }
      setPois((prev) => {
        if (prev.some((p) => p.id === next.id)) return prev
        return [next, ...prev]
      })
    })

    connection
      .start()
      .then(() => {
        if (cancelled) return
        setSignalrConnected(true)
      })
      .catch((e) => {
        if (cancelled) return
        setSignalrError(e?.message ?? String(e))
      })

    return () => {
      cancelled = true
      connection.stop().catch(() => {})
    }
  }, [])

  return (
    <>
      <section id="center">
        <div className="hero">
          <img src={heroImg} className="base" width="170" height="179" alt="" />
          <img src={reactLogo} className="framework" alt="React logo" />
          <img src={viteLogo} className="vite" alt="Vite logo" />
        </div>
        <div>
          <h1>Get started</h1>
          <p>
            Edit <code>src/App.tsx</code> and save to test <code>HMR</code>
          </p>
        </div>
        <button
          className="counter"
          onClick={() => setCount((count) => count + 1)}
        >
          Count is {count}
        </button>
      </section>

      <div className="ticks"></div>

      <section id="next-steps">
        <div style={{ marginBottom: 16 }}>
          <h2>API/Test</h2>
          <div>POI count: {pois.length}</div>
          <div>
            SignalR:{' '}
            {signalrConnected
              ? 'connected'
              : signalrError
                ? `error: ${signalrError}`
                : 'connecting...'}
          </div>
        </div>
        <div id="docs">
          <svg className="icon" role="presentation" aria-hidden="true">
            <use href="/icons.svg#documentation-icon"></use>
          </svg>
          <h2>Documentation</h2>
          <p>Your questions, answered</p>
          <ul>
            <li>
              <a href="https://vite.dev/" target="_blank">
                <img className="logo" src={viteLogo} alt="" />
                Explore Vite
              </a>
            </li>
            <li>
              <a href="https://react.dev/" target="_blank">
                <img className="button-icon" src={reactLogo} alt="" />
                Learn more
              </a>
            </li>
          </ul>
        </div>
        <div id="social">
          <svg className="icon" role="presentation" aria-hidden="true">
            <use href="/icons.svg#social-icon"></use>
          </svg>
          <h2>Connect with us</h2>
          <p>Join the Vite community</p>
          <ul>
            <li>
              <a href="https://github.com/vitejs/vite" target="_blank">
                <svg
                  className="button-icon"
                  role="presentation"
                  aria-hidden="true"
                >
                  <use href="/icons.svg#github-icon"></use>
                </svg>
                GitHub
              </a>
            </li>
            <li>
              <a href="https://chat.vite.dev/" target="_blank">
                <svg
                  className="button-icon"
                  role="presentation"
                  aria-hidden="true"
                >
                  <use href="/icons.svg#discord-icon"></use>
                </svg>
                Discord
              </a>
            </li>
            <li>
              <a href="https://x.com/vite_js" target="_blank">
                <svg
                  className="button-icon"
                  role="presentation"
                  aria-hidden="true"
                >
                  <use href="/icons.svg#x-icon"></use>
                </svg>
                X.com
              </a>
            </li>
            <li>
              <a href="https://bsky.app/profile/vite.dev" target="_blank">
                <svg
                  className="button-icon"
                  role="presentation"
                  aria-hidden="true"
                >
                  <use href="/icons.svg#bluesky-icon"></use>
                </svg>
                Bluesky
              </a>
            </li>
          </ul>
        </div>
      </section>

      <div className="ticks"></div>
      <section id="spacer"></section>
    </>
  )
}

export default App
