import { writable, derived, get } from 'svelte/store';
import en from './locales/en.json';
import nl from './locales/nl.json';

const translations = { en, nl };

function interpolate(str, params) {
  if (!params || typeof str !== 'string') return str;
  return str.replace(/{(\w+)}/g, (_, k) => params[k] ?? `{${k}}`);
}

// Detects preferred locale from browser if no preference is stored
function getDefaultLocale() {
  const stored = localStorage.getItem('locale');
  if (stored && translations[stored]) return stored;
  return navigator.language?.startsWith('nl') ? 'nl' : 'en';
}

export const locale = writable(getDefaultLocale());

// Reactive t() for Svelte components: {$t('key')} or {$t('key', { name: 'foo' })}
export const t = derived(locale, $locale => (key, params) => {
  const str = translations[$locale]?.[key] ?? translations.en?.[key] ?? key;
  return interpolate(str, params);
});

// Non-reactive translate for use in plain JS files (hub.js, api.js)
export function tr(key, params) {
  const str = translations[get(locale)]?.[key] ?? translations.en?.[key] ?? key;
  return interpolate(str, params);
}

export function setLocale(loc) {
  if (translations[loc]) {
    locale.set(loc);
    localStorage.setItem('locale', loc);
  }
}
