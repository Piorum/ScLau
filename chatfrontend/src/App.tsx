import React, { useState } from 'react';
import './App.css';

function App() {
  const [message, setMessage] = useState('');

  const fetchData = async () => {
    setMessage('');
    const response = await fetch('/api/data');
    const reader = response.body?.getReader();
    const decoder = new TextDecoder();

    if (reader) {
      const read = async () => {
        const { done, value } = await reader.read();
        if (done) {
          return;
        }
        const chunk = decoder.decode(value, { stream: true });
        console.log('Received chunk:', chunk);
        setMessage(prevMessage => prevMessage + chunk);
        read();
      };
      read();
    }
  };

  return (
    <div className="App">
      <header className="App-header">
        <textarea value={message} readOnly rows={10} cols={50} />
        <button onClick={fetchData}>Fetch Data</button>
      </header>
    </div>
  );
}

export default App;