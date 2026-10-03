(function () {
  'use strict';

  var STORAGE_KEY = 'cms.adminBar.collapsed';
  var bar = document.querySelector('[data-admin-bar]');
  if (!bar) return;

  var restore = document.querySelector('[data-admin-bar-restore]');
  var collapseBtn = bar.querySelector('[data-admin-bar-collapse]');
  var menus = Array.prototype.slice.call(bar.querySelectorAll('[data-admin-bar-menu]'));
  var root = document.documentElement;

  function setCollapsed(collapsed) {
    bar.classList.toggle('is-collapsed', collapsed);
    bar.setAttribute('aria-hidden', collapsed ? 'true' : 'false');
    if (restore) restore.hidden = !collapsed;
    if (collapseBtn) collapseBtn.setAttribute('aria-expanded', collapsed ? 'false' : 'true');
    root.style.setProperty('--site-admin-bar-height', collapsed ? '0px' : '');
    try {
      sessionStorage.setItem(STORAGE_KEY, collapsed ? '1' : '0');
    } catch (e) { /* private mode */ }
    closeAllMenus();
  }

  function closeMenu(menu) {
    var trigger = menu.querySelector('[data-admin-bar-menu-trigger]');
    var panel = menu.querySelector('[data-admin-bar-menu-panel]');
    if (!trigger || !panel) return;
    panel.hidden = true;
    trigger.setAttribute('aria-expanded', 'false');
    menu.classList.remove('is-open');
  }

  function closeAllMenus(except) {
    menus.forEach(function (menu) {
      if (except && menu === except) return;
      closeMenu(menu);
    });
  }

  function openMenu(menu) {
    var trigger = menu.querySelector('[data-admin-bar-menu-trigger]');
    var panel = menu.querySelector('[data-admin-bar-menu-panel]');
    if (!trigger || !panel) return;
    closeAllMenus(menu);
    panel.hidden = false;
    trigger.setAttribute('aria-expanded', 'true');
    menu.classList.add('is-open');
  }

  function toggleMenu(menu) {
    var panel = menu.querySelector('[data-admin-bar-menu-panel]');
    if (!panel) return;
    if (panel.hidden) openMenu(menu);
    else closeMenu(menu);
  }

  menus.forEach(function (menu) {
    var trigger = menu.querySelector('[data-admin-bar-menu-trigger]');
    if (!trigger) return;
    trigger.addEventListener('click', function (ev) {
      ev.preventDefault();
      ev.stopPropagation();
      toggleMenu(menu);
    });
  });

  document.addEventListener('click', function (ev) {
    if (!bar.contains(ev.target)) closeAllMenus();
  });

  document.addEventListener('keydown', function (ev) {
    if (ev.key === 'Escape') {
      closeAllMenus();
      if (bar.classList.contains('is-collapsed') && restore) restore.focus();
    }
  });

  collapseBtn?.addEventListener('click', function (ev) {
    ev.preventDefault();
    setCollapsed(true);
    restore?.focus();
  });

  restore?.addEventListener('click', function (ev) {
    ev.preventDefault();
    setCollapsed(false);
    collapseBtn?.focus();
  });

  var initiallyCollapsed = false;
  try {
    initiallyCollapsed = sessionStorage.getItem(STORAGE_KEY) === '1';
  } catch (e) { /* ignore */ }

  if (initiallyCollapsed) setCollapsed(true);
})();
