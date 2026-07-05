/* <iso-city> — procedural isometric city background.
   Faithful to CityBuilder.Core PlaceholderSpriteFactory palette:
   grass(96,168,88) water(48,96,200) road(60,60,66)
   res(64,180,96) com(72,128,220) ind(220,196,72) civic(180,90,200)
   Attrs: theme="day|night|blueprint" vehicles="on|off" density="0..1" seed="int" */
(function () {
  if (customElements.get('iso-city')) return;

  function mulberry32(a) {
    return function () {
      a |= 0; a = (a + 0x6D2B79F5) | 0;
      var t = Math.imul(a ^ (a >>> 15), 1 | a);
      t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
  }
  function hash(x, y, s) {
    var n = ((x * 73856093) ^ (y * 19349663) ^ (s * 83492791)) >>> 0;
    n = Math.imul(n ^ (n >>> 13), 1274126177);
    return (((n ^ (n >>> 16)) >>> 0) % 100000) / 100000;
  }
  function shade(c, f) { return [c[0] * f | 0, c[1] * f | 0, c[2] * f | 0]; }
  function lerp(a, b, t) {
    return [a[0] + (b[0] - a[0]) * t | 0, a[1] + (b[1] - a[1]) * t | 0, a[2] + (b[2] - a[2]) * t | 0];
  }
  function css(c, a) { return 'rgba(' + c[0] + ',' + c[1] + ',' + c[2] + ',' + (a == null ? 1 : a) + ')'; }

  var PAL = {
    grass: [96, 168, 88], water: [48, 96, 200], road: [60, 60, 66],
    res: [64, 180, 96], com: [72, 128, 220], ind: [220, 196, 72], civ: [180, 90, 200]
  };
  var VEH = [[230, 230, 230], [230, 230, 230], [200, 120, 40], [220, 60, 60], [240, 220, 60]];
  var WHITE = [255, 255, 255];

  class IsoCity extends HTMLElement {
    static get observedAttributes() { return ['theme', 'vehicles', 'density', 'seed']; }

    connectedCallback() {
      if (this._init) return;
      this._init = true;
      this.style.display = 'block';
      this._c = document.createElement('canvas');
      this._c.style.cssText = 'width:100%;height:100%;display:block';
      this.appendChild(this._c);
      this._ro = new ResizeObserver(this._layout.bind(this));
      this._ro.observe(this);
      this._layout();
      this._tick = this._tick.bind(this);
      this._raf = requestAnimationFrame(this._tick);
    }
    disconnectedCallback() {
      cancelAnimationFrame(this._raf);
      if (this._ro) this._ro.disconnect();
    }
    attributeChangedCallback() {
      if (this._init && this._ctx) { this._build(); this._draw(performance.now()); }
    }

    _layout() {
      var r = this.getBoundingClientRect();
      if (r.width < 2 || r.height < 2) return;
      var d = Math.min(2, window.devicePixelRatio || 1);
      this._w = r.width; this._h = r.height;
      this._c.width = r.width * d; this._c.height = r.height * d;
      this._ctx = this._c.getContext('2d');
      this._ctx.setTransform(d, 0, 0, d, 0, 0);
      this._build();
      this._draw(performance.now());
    }

    _build() {
      var seed = parseInt(this.getAttribute('seed') || '7', 10) || 7;
      var density = Math.max(0, Math.min(1, parseFloat(this.getAttribute('density') || '0.75')));
      var rng = mulberry32((seed * 2654435761) >>> 0 || 7);
      var hw = this._hw = 34, hh = this._hh = 17;
      var N = this._N = Math.ceil(Math.max(this._w / (2 * hw), this._h / (2 * hh)) * 1.3) + 4;
      var rx = this._rx = Math.floor(rng() * 4);
      var ry = this._ry = Math.floor(rng() * 5);

      var blobs = [];
      for (var i = 0; i < 2; i++) blobs.push([rng() * N, rng() * N, 2.5 + rng() * 3.5]);

      var cells = this._cells = new Array(N * N);
      var cx0 = N / 2, maxD = N * 0.62;
      for (var y = 0; y < N; y++) for (var x = 0; x < N; x++) {
        var cell = { k: 'grass', z: null, dev: 0 };
        for (var b = 0; b < 2; b++) {
          var dx = x - blobs[b][0], dy = y - blobs[b][1];
          if (Math.sqrt(dx * dx + dy * dy) < blobs[b][2] + (hash(x, y, seed + b) - 0.5) * 1.6) { cell.k = 'water'; break; }
        }
        if (cell.k !== 'water') {
          if ((x + rx) % 5 === 0 || (y + ry) % 6 === 0) cell.k = 'road';
          else {
            var d = hash(Math.floor(x / 5), Math.floor(y / 6), seed + 9);
            cell.z = d < 0.42 ? 'res' : d < 0.68 ? 'com' : d < 0.88 ? 'ind' : 'civ';
            if (hash(x, y, seed + 3) > density) { cell.z = null; }
            else {
              var ddx = x - cx0, ddy = y - cx0;
              var cf = 1 - Math.sqrt(ddx * ddx + ddy * ddy) / maxD;
              var dev = Math.max(0, Math.min(1, cf * 0.75 + (hash(x, y, seed + 5) - 0.3) * 0.6));
              cell.dev = Math.round(dev * 255);
            }
          }
        }
        cells[y * N + x] = cell;
      }

      var cols = [], rows = [];
      for (var k = 0; k < N; k++) {
        if ((k + rx) % 5 === 0) cols.push(k);
        if ((k + ry) % 6 === 0) rows.push(k);
      }
      var veh = this._veh = [];
      for (var v = 0; v < 12; v++) {
        var axis = v % 2 === 0 && cols.length ? 'y' : 'x';
        var line = axis === 'y' ? cols[Math.floor(rng() * cols.length)] : rows[Math.floor(rng() * rows.length)];
        if (line == null) continue;
        veh.push({
          axis: axis, line: line, phase: rng(), dir: rng() > 0.5 ? 1 : -1,
          speed: 0.5 + rng() * 0.9,
          color: VEH[Math.floor(rng() * VEH.length)]
        });
      }
    }

    _dia(cx, cy, hw, hh) {
      var g = this._ctx;
      g.beginPath();
      g.moveTo(cx, cy - hh); g.lineTo(cx + hw, cy); g.lineTo(cx, cy + hh); g.lineTo(cx - hw, cy);
      g.closePath();
    }

    _draw(t) {
      var g = this._ctx; if (!g) return;
      var th = this.getAttribute('theme') || 'day';
      var w = this._w, h = this._h, N = this._N, hw = this._hw, hh = this._hh;
      var seed = parseInt(this.getAttribute('seed') || '7', 10) || 7;
      var bp = th === 'blueprint', night = th === 'night';

      g.clearRect(0, 0, w, h);
      g.fillStyle = bp ? '#12395f' : night ? '#0a0e15' : '#d3ddcc';
      g.fillRect(0, 0, w, h);

      var ox = w / 2, oy = h / 2 - (N - 1) * hh;
      var line = 'rgba(196,224,252,';

      for (var s = 0; s <= 2 * N - 2; s++) {
        var x0 = Math.max(0, s - N + 1), x1 = Math.min(N - 1, s);
        for (var x = x0; x <= x1; x++) {
          var y = s - x;
          var cell = this._cells[y * N + x];
          var sx = ox + (x - y) * hw, sy = oy + (x + y) * hh;
          if (sx < -hw * 2 || sx > w + hw * 2 || sy < -60 || sy > h + hh * 2) continue;

          if (bp) {
            this._dia(sx, sy, hw, hh);
            if (cell.k === 'water') { g.fillStyle = line + '0.16)'; g.fill(); }
            else if (cell.k === 'road') { g.fillStyle = line + '0.10)'; g.fill(); }
            else if (cell.z) { g.fillStyle = css(PAL[cell.z], 0.07); g.fill(); }
            g.strokeStyle = line + (cell.k === 'road' ? '0.5)' : '0.22)');
            g.lineWidth = 0.75;
            g.stroke();
            if (cell.dev >= 16) this._prismBP(sx, sy, hw, hh, 4 + (cell.dev / 255) * 38, line);
            continue;
          }

          var base;
          if (cell.k === 'water') base = night ? shade(PAL.water, 0.42) : PAL.water;
          else if (cell.k === 'road') base = night ? shade(PAL.road, 0.55) : PAL.road;
          else {
            var gs = 0.94 + hash(x, y, seed + 21) * 0.12;
            base = shade(PAL.grass, night ? 0.24 * gs : gs);
          }
          this._dia(sx, sy, hw, hh);
          g.fillStyle = css(base);
          g.fill();
          if (!night) { g.strokeStyle = 'rgba(20,20,28,0.22)'; g.lineWidth = 0.7; g.stroke(); }

          if (cell.z && cell.dev < 16) {
            this._dia(sx, sy, hw, hh);
            g.fillStyle = css(PAL[cell.z], night ? 0.28 : 0.45);
            g.fill();
          } else if (cell.dev >= 16) {
            var hgt = 4 + (cell.dev / 255) * 38;
            var zc = PAL[cell.z];
            var top = night ? shade(lerp(zc, WHITE, cell.dev / 512), 0.42) : lerp(zc, WHITE, cell.dev / 512);
            var lf = shade(zc, night ? 0.2 : 0.6), rf = shade(zc, night ? 0.3 : 0.8);
            g.beginPath();
            g.moveTo(sx - hw, sy); g.lineTo(sx, sy + hh); g.lineTo(sx, sy + hh - hgt); g.lineTo(sx - hw, sy - hgt);
            g.closePath(); g.fillStyle = css(lf); g.fill();
            g.beginPath();
            g.moveTo(sx + hw, sy); g.lineTo(sx, sy + hh); g.lineTo(sx, sy + hh - hgt); g.lineTo(sx + hw, sy - hgt);
            g.closePath(); g.fillStyle = css(rf); g.fill();
            this._dia(sx, sy - hgt, hw, hh);
            g.fillStyle = css(top); g.fill();
            if (!night) { g.strokeStyle = 'rgba(20,20,28,0.3)'; g.lineWidth = 0.7; g.stroke(); }
            if (night && hgt > 12) {
              var rowsN = Math.floor(hgt / 7);
              for (var i = 0; i < rowsN; i++) for (var f = -1; f <= 1; f += 2) {
                if (hash(x + f * 7, y + i * 3, seed + 13) < 0.45) continue;
                var px = sx + f * hw * 0.45, py = sy + hh * 0.4 - hgt + 5 + i * 6;
                g.fillStyle = 'rgba(255,208,116,' + (0.5 + hash(x, y + i, f + 2) * 0.5) + ')';
                g.fillRect(px - 1, py, 2.4, 2.4);
              }
            }
          }
        }
      }

      if ((this.getAttribute('vehicles') || 'on') !== 'off') {
        for (var v = 0; v < this._veh.length; v++) {
          var vv = this._veh[v];
          var p = ((t * 0.00003 * vv.speed * vv.dir + vv.phase) % 1 + 1) % 1;
          var vx = vv.axis === 'y' ? vv.line : p * (N - 1);
          var vy = vv.axis === 'y' ? p * (N - 1) : vv.line;
          var cxi = Math.min(N - 1, Math.max(0, Math.round(vx)));
          var cyi = Math.min(N - 1, Math.max(0, Math.round(vy)));
          if (this._cells[cyi * N + cxi].k === 'water') continue;
          var vsx = ox + (vx - vy) * hw, vsy = oy + (vx + vy) * hh - 3;
          if (vsx < -20 || vsx > w + 20 || vsy < -20 || vsy > h + 20) continue;
          if (night) { g.shadowColor = css(vv.color, 0.9); g.shadowBlur = 8; }
          this._dia(vsx, vsy, 6, 3);
          g.fillStyle = bp ? 'rgba(228,242,255,0.9)' : css(night ? lerp(vv.color, WHITE, 0.2) : vv.color);
          g.fill();
          g.shadowBlur = 0;
        }
      }
    }

    _prismBP(sx, sy, hw, hh, hgt, line) {
      var g = this._ctx;
      this._dia(sx, sy - hgt, hw, hh);
      g.fillStyle = line + '0.06)'; g.fill();
      g.strokeStyle = line + '0.7)'; g.lineWidth = 0.9; g.stroke();
      g.beginPath();
      g.moveTo(sx - hw, sy); g.lineTo(sx - hw, sy - hgt);
      g.moveTo(sx + hw, sy); g.lineTo(sx + hw, sy - hgt);
      g.moveTo(sx, sy + hh); g.lineTo(sx, sy + hh - hgt);
      g.stroke();
    }

    _tick(t) {
      if ((this.getAttribute('vehicles') || 'on') !== 'off') this._draw(t);
      this._raf = requestAnimationFrame(this._tick);
    }
  }
  customElements.define('iso-city', IsoCity);
})();
