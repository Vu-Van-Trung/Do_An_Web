document.addEventListener("DOMContentLoaded", () => {
    // 1. Inject HTML for Chatbot
    const chatbotHTML = `
        <div id="chatbot-container">
            <button id="chatbot-toggle" title="Chat with AI">
                <i class="bi bi-chat-dots-fill"></i>
            </button>
            <div id="chatbot-window">
                <div id="chatbot-header">
                    <span><i class="bi bi-robot"></i> NexusGear AI</span>
                    <button id="chatbot-close"><i class="bi bi-x-lg"></i></button>
                </div>
                <div id="chatbot-messages">
                    <div class="chat-msg bot">Xin chào! Tôi là trợ lý ảo của NexusGear. Tôi có thể giúp gì cho bạn trong việc chọn mua thiết bị gaming hôm nay?</div>
                </div>
                <div id="chatbot-input-area">
                    <input type="text" id="chatbot-input" placeholder="Nhập tin nhắn..." autocomplete="off" />
                    <button id="chatbot-send" disabled><i class="bi bi-send-fill"></i></button>
                </div>
            </div>
        </div>
    `;
    document.body.insertAdjacentHTML('beforeend', chatbotHTML);

    const toggleBtn = document.getElementById('chatbot-toggle');
    const closeBtn = document.getElementById('chatbot-close');
    const chatWindow = document.getElementById('chatbot-window');
    const chatMessages = document.getElementById('chatbot-messages');
    const chatInput = document.getElementById('chatbot-input');
    const sendBtn = document.getElementById('chatbot-send');

    let chatHistory = [];
    let isWaiting = false;

    // Toggle logic
    toggleBtn.addEventListener('click', () => {
        chatWindow.classList.add('open');
        chatInput.focus();
    });

    closeBtn.addEventListener('click', () => {
        chatWindow.classList.remove('open');
    });

    // Input state
    chatInput.addEventListener('input', () => {
        sendBtn.disabled = chatInput.value.trim() === '' || isWaiting;
    });

    chatInput.addEventListener('keypress', (e) => {
        if (e.key === 'Enter' && !sendBtn.disabled) {
            sendMessage();
        }
    });

    sendBtn.addEventListener('click', () => {
        if (!sendBtn.disabled) sendMessage();
    });

    function appendMessage(role, content) {
        const msgDiv = document.createElement('div');
        msgDiv.className = `chat-msg ${role}`;
        msgDiv.textContent = content;
        chatMessages.appendChild(msgDiv);
        chatMessages.scrollTop = chatMessages.scrollHeight;
        return msgDiv;
    }

    function showTyping() {
        const typingDiv = document.createElement('div');
        typingDiv.className = 'chat-msg bot typing';
        typingDiv.innerHTML = '<div class="typing-indicator"><span></span><span></span><span></span></div>';
        chatMessages.appendChild(typingDiv);
        chatMessages.scrollTop = chatMessages.scrollHeight;
        return typingDiv;
    }

    async function sendMessage() {
        const text = chatInput.value.trim();
        if (!text) return;

        // User message
        appendMessage('user', text);
        chatHistory.push({ role: 'user', content: text });
        chatInput.value = '';
        sendBtn.disabled = true;
        isWaiting = true;

        const typingIndicator = showTyping();

        try {
            const response = await fetch('/api/chat/stream', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ messages: chatHistory })
            });

            if (!response.ok) {
                throw new Error('Network error');
            }

            chatMessages.removeChild(typingIndicator);
            const botMsgDiv = appendMessage('bot', '');
            let botText = '';

            const reader = response.body.getReader();
            const decoder = new TextDecoder('utf-8');
            let buffer = '';

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;
                
                buffer += decoder.decode(value, { stream: true });
                const lines = buffer.split('\n');
                buffer = lines.pop(); // Keep incomplete line

                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        const dataStr = line.substring(6).trim();
                        if (dataStr === '[DONE]') {
                            break;
                        }
                        try {
                            const data = JSON.parse(dataStr);
                            if (data.error) {
                                botText += "\\n[Lỗi: " + data.error + "]";
                            } else {
                                botText += data;
                            }
                            botMsgDiv.textContent = botText;
                            chatMessages.scrollTop = chatMessages.scrollHeight;
                        } catch (e) {
                            // ignore parse error for chunk
                        }
                    }
                }
            }

            chatHistory.push({ role: 'assistant', content: botText });

        } catch (error) {
            chatMessages.removeChild(typingIndicator);
            appendMessage('bot', 'Xin lỗi, không thể kết nối đến máy chủ AI lúc này.');
        } finally {
            isWaiting = false;
            sendBtn.disabled = chatInput.value.trim() === '';
            chatInput.focus();
        }
    }
});
