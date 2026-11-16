const $ = id => document.getElementById(id);
const API_BASE_URL = window.AUTH_API_BASE_URL.replace(/\/+$/, '');
const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/, PHONE = /^(0|\+84)\d{9}$/;
const isContact = v => EMAIL.test(v) || PHONE.test(v.replace(/[\s.-]/g, ''));
const strongPw = v => v.length >= 8 && /[A-Za-z]/.test(v) && /\d/.test(v);

function setErr(id, text){ $(id + '-e').textContent = text || ''; $(id).classList.toggle('bad', !!text); return !text; }
function msg(id, text){ const m = $(id); m.textContent = text || ''; m.classList.toggle('on', !!text); }

async function postJson(url, body){
  const response = await fetch(url, {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    credentials: 'include',
    body: JSON.stringify(body)
  });
  const data = response.headers.get('content-type')?.includes('application/json')
    ? await response.json()
    : {};
  return {response, data};
}

function setBusy(form, busy){
  const button = form.querySelector('button[type="submit"]');
  button.disabled = busy;
  button.setAttribute('aria-busy', busy);
}

function tab(reg){
  $('tab-login').setAttribute('aria-selected', !reg);
  $('tab-reg').setAttribute('aria-selected', reg);
  $('f-login').hidden = reg;
  $('f-reg').hidden = !reg;
}
$('tab-login').onclick = () => tab(false);
$('tab-reg').onclick = () => tab(true);
document.querySelectorAll('[data-tab]').forEach(b => b.onclick = () => tab(b.dataset.tab === 'reg'));

document.querySelectorAll('[data-toggle]').forEach(b => b.onclick = () => {
  const i = $(b.dataset.toggle), hidden = i.type === 'password';
  i.type = hidden ? 'text' : 'password';
  b.textContent = hidden ? 'Ẩn' : 'Hiện';
});

/* Đăng nhập */
$('f-login').onsubmit = async e => {
  e.preventDefault(); msg('l-msg');
  const form = e.currentTarget;
  const id = $('l-id').value.trim(), pw = $('l-pw').value;
  const a = setErr('l-id', id ? (isContact(id) ? '' : 'Nhập email hoặc số điện thoại hợp lệ.') : 'Vui lòng nhập email hoặc số điện thoại.');
  const b = setErr('l-pw', pw ? '' : 'Vui lòng nhập mật khẩu.');
  if (!(a && b)) return;
  setBusy(form, true);
  try {
    const {response, data} = await postJson(`${API_BASE_URL}/api/auth/login`, {identifier:id, password:pw});
    if (response.ok) {
      msg('l-msg', data.message || 'Đăng nhập thành công.');
    } else if (response.status === 423) {
      msg('l-msg', data.message || 'Tài khoản bị khóa. Vui lòng thử lại sau 15 phút.');
    } else {
      msg('l-msg', data.message || 'Email/số điện thoại hoặc mật khẩu không đúng.');
    }
  } catch {
    msg('l-msg', 'Không thể kết nối máy chủ. Hãy kiểm tra API ASP.NET Core đang chạy.');
  } finally {
    setBusy(form, false);
  }
};

/* Đăng ký */
$('f-reg').onsubmit = async e => {
  e.preventDefault(); msg('r-msg');
  const form = e.currentTarget;
  const name = $('r-name').value.trim(), id = $('r-id').value.trim(), pw = $('r-pw').value, pw2 = $('r-pw2').value;
  const ok = [
    setErr('r-name', name ? '' : 'Vui lòng nhập họ và tên.'),
    setErr('r-id', id ? (isContact(id) ? '' : 'Nhập email hoặc số điện thoại hợp lệ.') : 'Vui lòng nhập email hoặc số điện thoại.'),
    setErr('r-pw', strongPw(pw) ? '' : 'Mật khẩu tối thiểu 8 ký tự, gồm chữ và số.'),
    setErr('r-pw2', pw2 === pw ? '' : 'Mật khẩu nhập lại chưa khớp.')
  ];
  $('r-ok-e').textContent = $('r-ok').checked ? '' : 'Bạn cần đồng ý điều khoản để tiếp tục.';
  if (ok.includes(false) || !$('r-ok').checked) return;
  setBusy(form, true);
  try {
    const {response, data} = await postJson(`${API_BASE_URL}/api/auth/register`, {
      fullName:name,
      identifier:id,
      password:pw
    });
    if (response.ok) {
      $('l-id').value = id;
      form.reset();
      tab(false);
      msg('l-msg', data.message || 'Đăng ký thành công. Vui lòng đăng nhập.');
    } else {
      msg('r-msg', data.message || 'Không thể đăng ký tài khoản.');
    }
  } catch {
    msg('r-msg', 'Không thể kết nối máy chủ. Hãy kiểm tra API ASP.NET Core đang chạy.');
  } finally {
    setBusy(form, false);
  }
};
