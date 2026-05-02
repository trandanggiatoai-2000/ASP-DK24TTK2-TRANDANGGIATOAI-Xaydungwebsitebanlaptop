(function(){
  const slider = document.querySelector('[data-slider]');
  if (slider) {
    const slides = slider.querySelectorAll('.hero-slide, .hero-campaign-slide');
    const dots = slider.querySelectorAll('[data-slide-index]');
    const prev = slider.querySelector('[data-slide-nav="prev"]');
    const next = slider.querySelector('[data-slide-nav="next"]');
    let index = 0;
    let timer = null;
    const show = (i) => {
      if (!slides.length) return;
      if (i < 0) i = slides.length - 1;
      if (i >= slides.length) i = 0;
      slides.forEach((s,idx)=>s.classList.toggle('active', idx===i));
      dots.forEach((d,idx)=>d.classList.toggle('active', idx===i));
      index = i;
    };
    const start = () => {
      if (timer) clearInterval(timer);
      if (slides.length > 1) timer = setInterval(()=> show(index + 1), 4500);
    };
    dots.forEach((dot)=> dot.addEventListener('click',()=> { show(parseInt(dot.dataset.slideIndex)); start(); }));
    prev?.addEventListener('click', ()=> { show(index - 1); start(); });
    next?.addEventListener('click', ()=> { show(index + 1); start(); });
    slider.addEventListener('mouseenter', ()=> timer && clearInterval(timer));
    slider.addEventListener('mouseleave', start);
    show(0);
    start();
  }

  document.querySelectorAll('.thumb-btn').forEach(btn => {
    btn.addEventListener('click', function(){
      const main = document.getElementById('mainProductImage');
      if(main) main.src = this.dataset.image;
    });
  });

  const toggle = document.getElementById('categoryToggle');
  const panel = document.getElementById('megaMenuPanel');
  if(toggle && panel){
    panel.classList.remove('show');
    toggle.addEventListener('click', function(e){
      e.stopPropagation();
      panel.classList.toggle('show');
    });
    panel.addEventListener('click', function(e){ e.stopPropagation(); });
    document.addEventListener('click', function(){ panel.classList.remove('show'); });
  }

  if (window.sitePopupEnabled) {
    const popupKey = 'ls_promo_popup_closed';
    let popupStorage = null;
    try { popupStorage = window.sessionStorage; } catch (err) { popupStorage = null; }

    const initPopup = () => {
      const backdrop = document.getElementById('promoPopupBackdrop');
      const popup = backdrop?.querySelector('.promo-popup');
      const close = document.getElementById('promoPopupClose');
      if (!backdrop || !popup) return;

      let isOpen = false;
      let timer = null;

      const hide = () => {
        isOpen = false;
        backdrop.hidden = true;
        backdrop.classList.remove('show');
        backdrop.style.display = 'none';
        backdrop.style.visibility = 'hidden';
        backdrop.style.opacity = '0';
        popup.style.pointerEvents = 'none';
        backdrop.setAttribute('aria-hidden', 'true');
        document.body.classList.remove('popup-open');
        if (popupStorage) popupStorage.setItem(popupKey, '1');
      };

      const show = () => {
        isOpen = true;
        backdrop.hidden = false;
        backdrop.classList.add('show');
        backdrop.style.display = 'grid';
        backdrop.style.visibility = 'visible';
        backdrop.style.opacity = '1';
        popup.style.pointerEvents = 'auto';
        backdrop.setAttribute('aria-hidden', 'false');
        document.body.classList.add('popup-open');
      };

      window.closePromoPopup = hide;
      window.openPromoPopup = show;

      const isClosed = popupStorage && popupStorage.getItem(popupKey) === '1';
      if (isClosed) { hide(); }
      else { timer = window.setTimeout(show, Number(window.sitePopupDelay || 700)); }

      close?.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        hide();
      });

      backdrop.addEventListener('click', function (e) {
        if (e.target === backdrop) hide();
      });

      popup.addEventListener('click', function (e) {
        const closeBtn = e.target.closest('#promoPopupClose');
        if (closeBtn) {
          e.preventDefault();
          e.stopPropagation();
          hide();
        }
      });

      document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && isOpen) hide();
      });

      window.addEventListener('pagehide', function () {
        if (timer) window.clearTimeout(timer);
      }, { once: true });
    };

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initPopup, { once: true });
    else initPopup();
  }


  const sectionSwitcher = document.querySelector('[data-section-switcher]');
  if (sectionSwitcher) {
    const navItems = sectionSwitcher.querySelectorAll('[data-section-target]');
    const panels = sectionSwitcher.querySelectorAll('[data-section-panel]');
    const activatePanel = (key) => {
      navItems.forEach(btn => {
        const active = btn.dataset.sectionTarget === key;
        btn.classList.toggle('active', active);
        btn.setAttribute('aria-selected', active ? 'true' : 'false');
      });
      panels.forEach(panel => {
        const active = panel.dataset.sectionPanel === key;
        panel.classList.toggle('active', active);
        panel.hidden = !active;
      });
    };
    navItems.forEach(btn => btn.addEventListener('click', () => activatePanel(btn.dataset.sectionTarget)));
    const current = Array.from(navItems).find(x => x.classList.contains('active'))?.dataset.sectionTarget || navItems[0]?.dataset.sectionTarget;
    if (current) activatePanel(current);
  }

  const sortable = document.querySelector('[data-sortable-slides]');
  if (sortable) {
    let dragged = null;
    const updateOrder = () => {
      sortable.querySelectorAll('[data-slide-card]').forEach((card, index) => {
        const input = card.querySelector('[data-display-order]');
        const label = card.querySelector('[data-slide-order-label]');
        if (input) input.value = index + 1;
        if (label) label.textContent = index + 1;
      });
    };
    sortable.querySelectorAll('[data-slide-card]').forEach(card => {
      card.addEventListener('dragstart', () => { dragged = card; card.classList.add('dragging'); });
      card.addEventListener('dragend', () => { dragged = null; card.classList.remove('dragging'); updateOrder(); });
      card.addEventListener('dragover', (e) => {
        e.preventDefault();
        const current = e.currentTarget;
        if (!dragged || current === dragged) return;
        const rect = current.getBoundingClientRect();
        const after = (e.clientY - rect.top) > rect.height / 2;
        sortable.insertBefore(dragged, after ? current.nextSibling : current);
      });
    });
    updateOrder();
  }
})();

document.addEventListener('error', function(e){
  const target = e.target;
  if(target && target.tagName === 'IMG'){
    const fallback = target.getAttribute('data-fallback') || '/images/products/placeholder-generic.svg';
    if(target.getAttribute('src') !== fallback){
      target.setAttribute('src', fallback);
    }
  }
}, true);

(function(){
  const catalogRoot = document.querySelector('[data-catalog-ajax-root]');
  if (!catalogRoot) return;

  const form = catalogRoot.querySelector('[data-catalog-filter-form]');
  const results = catalogRoot.querySelector('[data-catalog-results]');
  const totalEl = catalogRoot.querySelector('[data-catalog-total]');
  if (!form || !results) return;

  const setHiddenToggle = (key, value) => {
    const input = form.querySelector(`[data-catalog-hidden="${key}"]`);
    if (input) input.value = value ? 'true' : 'false';
  };

  const syncStateFromUrl = (url) => {
    const u = new URL(url, window.location.origin);
    setHiddenToggle('official', u.searchParams.get('official') === 'true');
    setHiddenToggle('fast', u.searchParams.get('fast') === 'true');
    setHiddenToggle('installment', u.searchParams.get('installment') === 'true');
    ['keyword','brand','category','cpu','ram','ssd'].forEach(name => {
      const field = form.elements.namedItem(name);
      if (field) field.value = u.searchParams.get(name) || '';
    });
  };

  const bindCatalogLinks = () => {
    catalogRoot.querySelectorAll('[data-ajax-catalog-link]').forEach(link => {
      if (link.dataset.ajaxBound === '1') return;
      link.dataset.ajaxBound = '1';
      link.addEventListener('click', (e) => {
        e.preventDefault();
        const href = link.getAttribute('href');
        if (!href) return;
        syncStateFromUrl(href);
        refreshAjax(href);
      });
    });
  };

  const refreshAjax = async (url, push = true) => {
    const u = new URL(url, window.location.origin);
    u.searchParams.set('ajax', 'true');
    results.classList.add('is-loading');
    try {
      const response = await fetch(u.toString(), { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
      const html = await response.text();
      results.innerHTML = html;
      const totalText = results.querySelector('.catalog-top p')?.textContent || '';
      const matched = totalText.match(/(\d+) sản phẩm/);
      if (matched && totalEl) totalEl.textContent = `${matched[1]} sản phẩm`;
      if (push) {
        const browserUrl = new URL(url, window.location.origin);
        browserUrl.searchParams.delete('ajax');
        history.pushState({}, '', browserUrl.toString());
      }
      bindCatalogLinks();
    } catch (err) {
      window.location.href = url;
    } finally {
      results.classList.remove('is-loading');
    }
  };

  const formUrl = () => {
    const data = new FormData(form);
    const params = new URLSearchParams();
    for (const [key, value] of data.entries()) {
      if (value !== null && value !== '') params.set(key, String(value));
    }
    return `${form.action}?${params.toString()}`;
  };

  form.addEventListener('submit', (e) => {
    e.preventDefault();
    refreshAjax(formUrl());
  });

  form.querySelectorAll('select').forEach(sel => {
    sel.addEventListener('change', () => refreshAjax(formUrl()));
  });

  bindCatalogLinks();
  window.addEventListener('popstate', () => {
    syncStateFromUrl(window.location.href);
    refreshAjax(window.location.href, false);
  });
})();


document.addEventListener("DOMContentLoaded", function(){
  const bindImagePreview = (fileInputSelector, urlInputSelector, imgSelector) => {
    const fileInput = document.querySelector(fileInputSelector);
    const urlInput = document.querySelector(urlInputSelector);
    const img = document.querySelector(imgSelector);
    if (!img) return;
    if (fileInput) fileInput.addEventListener('change', function(){
      const file = this.files && this.files[0];
      if (!file) return;
      const url = URL.createObjectURL(file);
      img.src = url;
      if (urlInput) urlInput.value = '';
    });
    if (urlInput) urlInput.addEventListener('input', function(){
      if (this.value.trim()) img.src = this.value.trim();
    });
  };
  bindImagePreview('#Settings_WebsiteLogoFile', '#Settings_WebsiteLogoUrl', '[data-preview="websiteLogo"]');
});


(function(){
  const flashRoot = document.querySelector('[data-flash-sale]');
  if (!flashRoot) return;
  const endsAtRaw = flashRoot.getAttribute('data-countdown-ends-at');
  const countdown = flashRoot.querySelector('[data-flash-countdown]');
  if (!endsAtRaw || !countdown) return;
  const units = {
    weeks: countdown.querySelector('[data-unit="weeks"]'),
    hours: countdown.querySelector('[data-unit="hours"]'),
    minutes: countdown.querySelector('[data-unit="minutes"]'),
    seconds: countdown.querySelector('[data-unit="seconds"]')
  };
  const endDate = new Date(endsAtRaw);
  const pad = n => String(Math.max(0,n)).padStart(2,'0');
  function render(){
    let diff = endDate - new Date();
    if(diff<=0){
      Object.values(units).forEach(el=>el.textContent='00');
      return false;
    }
    let s = Math.floor(diff/1000);
    const w = Math.floor(s/604800); s-=w*604800;
    const h = Math.floor(s/3600); s-=h*3600;
    const m = Math.floor(s/60); s-=m*60;
    units.weeks.textContent=pad(w);
    units.hours.textContent=pad(h);
    units.minutes.textContent=pad(m);
    units.seconds.textContent=pad(s);
    return true;
  }
  render();
  const t=setInterval(()=>{ if(!render()) clearInterval(t); },1000);
})();


document.addEventListener('DOMContentLoaded', function () {
  const root = document.querySelector('[data-section-switcher]');
  if (!root) return;

  const navItems = Array.from(root.querySelectorAll('[data-section-target]'));
  const panels = Array.from(root.querySelectorAll('[data-section-panel]'));
  if (!navItems.length || !panels.length) return;

  const activatePanel = function (key) {
    navItems.forEach(function (btn) {
      const active = btn.getAttribute('data-section-target') === key;
      btn.classList.toggle('active', active);
      btn.setAttribute('aria-selected', active ? 'true' : 'false');
    });
    panels.forEach(function (panel) {
      const active = panel.getAttribute('data-section-panel') === key;
      panel.classList.toggle('active', active);
      panel.hidden = !active;
      panel.style.display = active ? 'block' : 'none';
    });
  };

  navItems.forEach(function (btn) {
    btn.addEventListener('click', function (e) {
      e.preventDefault();
      activatePanel(btn.getAttribute('data-section-target'));
    });
  });

  const firstActive = navItems.find(function (x) { return x.classList.contains('active'); });
  const initialKey = (firstActive && firstActive.getAttribute('data-section-target')) || navItems[0].getAttribute('data-section-target');
  activatePanel(initialKey);
});


(function(){
  const body = document.body;
  if (!body || !body.hasAttribute('data-admin-layout')) return;
  const storageKey = 'admin_sidebar_collapsed';
  const sidebar = document.getElementById('adminSidebar');
  const buttons = [
    document.getElementById('adminSidebarToggle'),
    document.getElementById('adminSidebarToggleTopbar'),
    document.getElementById('adminSidebarFloatToggle')
  ].filter(Boolean);
  if (!sidebar || !buttons.length) return;

  let collapsed = false;
  try { collapsed = window.localStorage.getItem(storageKey) === '1'; } catch (e) { collapsed = false; }

  const applyState = () => {
    body.classList.toggle('admin-sidebar-collapsed', collapsed);
    buttons.forEach(btn => {
      btn.setAttribute('aria-expanded', collapsed ? 'false' : 'true');
      const label = collapsed ? 'Mở danh mục quản lý' : 'Đóng danh mục quản lý';
      btn.setAttribute('title', label);
      const icon = btn.querySelector('.admin-sidebar-toggle-icon');
      if (icon && btn.id === 'adminSidebarFloatToggle') {
        icon.textContent = collapsed ? '☰' : '✕';
      }
    });
  };

  buttons.forEach(btn => btn.addEventListener('click', () => {
    collapsed = !collapsed;
    try { window.localStorage.setItem(storageKey, collapsed ? '1' : '0'); } catch (e) {}
    applyState();
  }));

  applyState();
})();
