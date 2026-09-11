import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App.jsx';
import './index.css';

// Render-crash safety net: without this, any component error unmounts the whole
// tree and the user sees a black screen with no hint of what went wrong.
class ErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = { error: null };
  }

  static getDerivedStateFromError(error) {
    return { error };
  }

  componentDidCatch(error, info) {
    console.error('Unhandled UI error:', error, info?.componentStack);
  }

  render() {
    if (this.state.error) {
      return (
        <div style={{ minHeight: '100vh', background: '#050510', color: '#f4f4f5', padding: '2rem', fontFamily: 'system-ui, sans-serif' }}>
          <h1 style={{ fontSize: '1.25rem', fontWeight: 600 }}>Something went wrong.</h1>
          <pre style={{ marginTop: '1rem', whiteSpace: 'pre-wrap', color: '#fca5a5', fontSize: '0.85rem' }}>
            {String(this.state.error?.message ?? this.state.error)}
          </pre>
          <button
            type="button"
            onClick={() => { this.setState({ error: null }); window.location.hash = ''; }}
            style={{ marginTop: '1rem', padding: '0.5rem 1rem', borderRadius: 8, background: '#22d3ee', color: '#09090b', fontWeight: 600, cursor: 'pointer' }}
          >
            Back to tracker
          </button>
        </div>
      );
    }
    return this.props.children;
  }
}

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <ErrorBoundary>
      <App />
    </ErrorBoundary>
  </React.StrictMode>
);
