<script>
  import { username, matchData, matchCountdown, isRematch, activeTag, gameMode } from '../stores.js';
  import { t } from '../i18n.js';
  import Banner from '../components/Banner.svelte';

  const themes = {
    ranked:  { bg: 'linear-gradient(160deg,#1a0535 0%,#2d0f5e 50%,#0d0820 100%)', accent: '#f59e0b', label: '⚔️ RANKED',       vs: '#f59e0b' },
    quick:   { bg: 'linear-gradient(160deg,#051628 0%,#0c2a4a 50%,#020d1a 100%)', accent: '#06b6d4', label: '⚡ QUICK MATCH',  vs: '#06b6d4' },
    private: { bg: 'linear-gradient(160deg,#0e0a30 0%,#1a1060 50%,#080520 100%)', accent: '#818cf8', label: '🔒 PRIVATE GAME', vs: '#818cf8' },
  };

  $: theme = themes[$gameMode] ?? themes.quick;

  $: countdownText = $matchCountdown > 1
    ? $t('countdown.starts_in', { n: $matchCountdown })
    : $matchCountdown === 1
      ? $t('countdown.starts_in_one', { n: $matchCountdown })
      : $t('countdown.starts_now');

  $: pct = ($matchCountdown / 5) * 100;
  $: circumference = 2 * Math.PI * 28;
  $: dash = (pct / 100) * circumference;
</script>

<div class="cd-screen" style="background: {theme.bg}">

  <div class="cd-mode-label" style="color:{theme.accent}; border-color:{theme.accent}33">
    {theme.label}
  </div>

  <p class="cd-title">{$isRematch ? $t('countdown.rematch') : $t('countdown.match_found')}</p>

  <div class="cd-players">
    <div class="cd-banner cd-banner-left">
      <Banner
        username={$matchData?.player1}
        pfp={$matchData?.player1Pfp}
        color={$matchData?.player1BannerColor ?? '#5b21b6'}
        tags={$matchData?.player1 === $username ? ($activeTag ? [$activeTag] : []) : ($matchData?.player1ActiveTag ? [$matchData.player1ActiveTag] : [])}
        isYou={$matchData?.player1 === $username}
        size="lg"
      />
    </div>

    <div class="cd-vs-col">
      <div class="cd-vs-ring" style="--vs-color:{theme.accent}">
        <svg width="72" height="72" viewBox="0 0 72 72">
          <circle cx="36" cy="36" r="28" fill="none" stroke="{theme.accent}22" stroke-width="3"/>
          <circle cx="36" cy="36" r="28" fill="none" stroke="{theme.accent}" stroke-width="3"
            stroke-dasharray="{dash} {circumference}" stroke-dashoffset="0"
            stroke-linecap="round" transform="rotate(-90 36 36)"
            style="transition: stroke-dasharray 1s linear"
          />
        </svg>
        <span class="cd-number" style="color:{$matchCountdown > 0 ? theme.accent : '#fff'}">
          {$matchCountdown > 0 ? $matchCountdown : '▶'}
        </span>
      </div>
      <span class="cd-vs-text" style="color:{theme.accent}">VS</span>
    </div>

    <div class="cd-banner cd-banner-right">
      <Banner
        username={$matchData?.player2}
        pfp={$matchData?.player2Pfp}
        color={$matchData?.player2BannerColor ?? '#5b21b6'}
        tags={$matchData?.player2 === $username ? ($activeTag ? [$activeTag] : []) : ($matchData?.player2ActiveTag ? [$matchData.player2ActiveTag] : [])}
        isYou={$matchData?.player2 === $username}
        size="lg"
      />
    </div>
  </div>

  <p class="cd-sub">{countdownText}</p>

</div>
