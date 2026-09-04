// 密碼欄的明碼切換。
// 掛法：把 <input type="password"> 包在 <span class="el-password"> 裡並加 data-el-password，
// 這支腳本會自己補上切換鈕——view 裡不需要寫按鈕，也不需要 onclick。
(function () {
  document.querySelectorAll('[data-el-password]').forEach(function (input) {
    var wrap = input.closest('.el-password');
    if (!wrap || wrap.querySelector('.el-password-toggle')) return;

    var btn = document.createElement('button');
    btn.type = 'button';                 // 不能是 submit，否則按下去會送出表單
    btn.className = 'el-password-toggle';
    btn.textContent = '顯示';
    btn.setAttribute('aria-label', '顯示密碼');
    // 螢幕閱讀器要知道這顆按鈕控制的是哪個欄位
    if (input.id) btn.setAttribute('aria-controls', input.id);
    btn.setAttribute('aria-pressed', 'false');

    btn.addEventListener('click', function () {
      var show = input.type === 'password';
      input.type = show ? 'text' : 'password';
      btn.textContent = show ? '隱藏' : '顯示';
      btn.setAttribute('aria-label', show ? '隱藏密碼' : '顯示密碼');
      btn.setAttribute('aria-pressed', String(show));
      // 切換後把游標還給輸入框，鍵盤使用者不用再 Tab 回去
      input.focus();
      var end = input.value.length;
      try { input.setSelectionRange(end, end); } catch (e) { /* type=text 以外可能不支援 */ }
    });

    wrap.appendChild(btn);
  });
})();
