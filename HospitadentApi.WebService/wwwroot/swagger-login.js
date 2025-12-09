(function() {
    'use strict';
    function createLoginForm() {
        const loginHtml = `
            <div id="swagger-login-container" style="
                position: fixed;
                top: 0;
                left: 0;
                right: 0;
                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                padding: 15px 20px;
                z-index: 10000;
                box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                display: flex;
                align-items: center;
                justify-content: space-between;
                flex-wrap: wrap;
                gap: 15px;
            ">
                <div style="display: flex; align-items: center; gap: 15px; flex-wrap: wrap;">
                    <div style="color: white; font-weight: bold; font-size: 16px;">🔐 API Girişi</div>
                    <input type="text" id="swagger-username" placeholder="Kullanıcı Adı" style="
                        padding: 8px 12px;
                        border: none;
                        border-radius: 4px;
                        font-size: 14px;
                        min-width: 150px;
                    ">
                    <input type="password" id="swagger-password" placeholder="Şifre" style="
                        padding: 8px 12px;
                        border: none;
                        border-radius: 4px;
                        font-size: 14px;
                        min-width: 150px;
                    ">
                    <button id="swagger-login-btn" style="
                        padding: 8px 20px;
                        background: #28a745;
                        color: white;
                        border: none;
                        border-radius: 4px;
                        cursor: pointer;
                        font-weight: bold;
                        font-size: 14px;
                        transition: background 0.3s;
                    " onmouseover="this.style.background='#218838'" onmouseout="this.style.background='#28a745'">
                        Giriş Yap
                    </button>
                    <button id="swagger-logout-btn" style="
                        padding: 8px 20px;
                        background: #dc3545;
                        color: white;
                        border: none;
                        border-radius: 4px;
                        cursor: pointer;
                        font-weight: bold;
                        font-size: 14px;
                        display: none;
                        transition: background 0.3s;
                    " onmouseover="this.style.background='#c82333'" onmouseout="this.style.background='#dc3545'">
                        Çıkış Yap
                    </button>
                </div>
                <div id="swagger-login-status" style="color: white; font-size: 14px;"></div>
            </div>
        `;
        
        document.body.insertAdjacentHTML('afterbegin', loginHtml);
                const swaggerUi = document.querySelector('.swagger-ui');
        if (swaggerUi) {
            swaggerUi.style.marginTop = '70px';
        }
    }
        function setSwaggerToken(token) {
        if (typeof ui !== 'undefined' && ui.authActions) {
            ui.authActions.authorize({
                Bearer: {
                    name: 'Bearer',
                    schema: {
                        type: 'apiKey',
                        name: 'Authorization',
                        in: 'header'
                    },
                    value: 'Bearer ' + token
                }
            });
        } else {
            // Alternatif yöntem: localStorage kullan
            localStorage.setItem('swagger_token', token);
            
            // Authorization header'ı manuel olarak ayarla
            const authInput = document.querySelector('input[placeholder*="Bearer"]');
            if (authInput) {
                authInput.value = 'Bearer ' + token;
                authInput.dispatchEvent(new Event('input', { bubbles: true }));
            }
        }
    }
    async function performLogin() {
        const username = document.getElementById('swagger-username').value;
        const password = document.getElementById('swagger-password').value;
        const statusDiv = document.getElementById('swagger-login-status');
        const loginBtn = document.getElementById('swagger-login-btn');
        
        if (!username || !password) {
            statusDiv.textContent = '⚠️ Lütfen kullanıcı adı ve şifre girin';
            statusDiv.style.color = '#ffc107';
            return;
        }

        loginBtn.disabled = true;
        loginBtn.textContent = 'Giriş yapılıyor...';
        statusDiv.textContent = '⏳ Giriş yapılıyor...';
        statusDiv.style.color = 'white';

        try {
            const response = await fetch('/api/Auth/login', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    username: username,
                    password: password
                })
            });

            const data = await response.json();

            if (response.ok && data.token) {
                localStorage.setItem('jwt_token', data.token);
                localStorage.setItem('user_info', JSON.stringify(data.user));
                setSwaggerToken(data.token);
                document.getElementById('swagger-username').style.display = 'none';
                document.getElementById('swagger-password').style.display = 'none';
                loginBtn.style.display = 'none';
                document.getElementById('swagger-logout-btn').style.display = 'inline-block';
                
                statusDiv.textContent = `✅ Hoş geldiniz, ${data.user.name} ${data.user.lastName}`;
                statusDiv.style.color = '#90EE90';
                
                setTimeout(() => {
                    window.location.reload();
                }, 1000);
            } else {
                statusDiv.textContent = '❌ ' + (data.message || 'Giriş başarısız');
                statusDiv.style.color = '#ff6b6b';
                loginBtn.disabled = false;
                loginBtn.textContent = 'Giriş Yap';
            }
        } catch (error) {
            statusDiv.textContent = '❌ Bağlantı hatası: ' + error.message;
            statusDiv.style.color = '#ff6b6b';
            loginBtn.disabled = false;
            loginBtn.textContent = 'Giriş Yap';
        }
    }

    // Logout işlemi
    function performLogout() {
        localStorage.removeItem('jwt_token');
        localStorage.removeItem('user_info');
        
        // Swagger UI'dan token'ı kaldır
        if (typeof ui !== 'undefined' && ui.authActions) {
            ui.authActions.logout(['Bearer']);
        }
        
        // UI'ı sıfırla
        document.getElementById('swagger-username').style.display = 'inline-block';
        document.getElementById('swagger-password').style.display = 'inline-block';
        document.getElementById('swagger-login-btn').style.display = 'inline-block';
        document.getElementById('swagger-logout-btn').style.display = 'none';
        document.getElementById('swagger-username').value = '';
        document.getElementById('swagger-password').value = '';
        document.getElementById('swagger-login-status').textContent = '';
        
        window.location.reload();
    }

    function init() {
        createLoginForm();
        
        document.getElementById('swagger-login-btn').addEventListener('click', performLogin);
        document.getElementById('swagger-logout-btn').addEventListener('click', performLogout);
        
        document.getElementById('swagger-password').addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                performLogin();
            }
        });
        
        const savedToken = localStorage.getItem('jwt_token');
        if (savedToken) {
            const userInfo = JSON.parse(localStorage.getItem('user_info') || '{}');
            document.getElementById('swagger-username').style.display = 'none';
            document.getElementById('swagger-password').style.display = 'none';
            document.getElementById('swagger-login-btn').style.display = 'none';
            document.getElementById('swagger-logout-btn').style.display = 'inline-block';
            document.getElementById('swagger-login-status').textContent = `✅ Giriş yapıldı: ${userInfo.name || ''} ${userInfo.lastName || ''}`;
            document.getElementById('swagger-login-status').style.color = '#90EE90';
            
            // Token'ı Swagger UI'a ekle
            setTimeout(() => {
                setSwaggerToken(savedToken);
            }, 500);
        }
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        setTimeout(init, 500); 
    }
})();

