// 密碼強度的三格與規則勾勾。純顯示用，真正的規則由 ASP.NET Identity 驗。
(function () {
  var input = document.querySelector('[data-el-strength]');
  if (!input) return;
  var bars = document.querySelectorAll('[data-el-strength-bars] span');
  var rules = {
    len: function (v) { return v.length >= 8; },
    case: function (v) { return /[a-z]/.test(v) && /[A-Z]/.test(v); },
    num: function (v) { return /[0-9]/.test(v); }
  };

  function paint() {
    var v = input.value, score = 0;
    Object.keys(rules).forEach(function (key) {
      var pass = rules[key](v);
      if (pass) score++;
      var el = document.querySelector('[data-el-rule="' + key + '"]');
      if (el) el.textContent = (pass ? '✓' : '·') + el.textContent.slice(1);
    });
    bars.forEach(function (bar, i) { bar.classList.toggle('on', i < score); });
  }

  input.addEventListener('input', paint);
  paint();
})();
