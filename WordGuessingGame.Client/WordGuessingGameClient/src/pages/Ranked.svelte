<script>
  import { onMount, onDestroy } from 'svelte';
  import { username, profilePicUrl, activeTag, isWaiting, rankedStats, page } from '../stores.js';
  import { connectToRanked, cancelRanked } from '../hub.js';
  import { fetchRankedStats, fetchRankedHistory } from '../api.js';
  import { t } from '../i18n.js';

  let matchHistory = [];
  let queueSecs = 0;
  let queueInterval = null;
  let tipIndex = 0;
  let tipInterval = null;

  const tips = ['ranked.tip_1', 'ranked.tip_2', 'ranked.tip_3'];

  const floatingLetters = [
    { l:'⚔', x:7,  y:12, s:2,   d:8,  o:0.05 },
    { l:'★', x:90, y:20, s:1.5, d:11, o:0.06 },
    { l:'◆', x:15, y:75, s:2.5, d:7,  o:0.04 },
    { l:'⚔', x:82, y:65, s:1.8, d:9,  o:0.05 },
    { l:'★', x:50, y:8,  s:1.5, d:13, o:0.04 },
    { l:'◆', x:93, y:85, s:2,   d:6,  o:0.05 },
    { l:'★', x:5,  y:88, s:1.6, d:10, o:0.04 },
  ];

  onMount(async () => {
    try { const rs = await fetchRankedStats(); if (rs) rankedStats.set(rs); } catch {}
    try { matchHistory = await fetchRankedHistory(); } catch {}
  });

  onDestroy(() => {
    if (queueInterval) clearInterval(queueInterval);
    if (tipInterval)   clearInterval(tipInterval);
  });

  $: if ($isWaiting) {
    if (!queueInterval) { queueSecs = 0; queueInterval = setInterval(() => queueSecs++, 1000); }
    if (!tipInterval)   { tipInterval = setInterval(() => tipIndex = (tipIndex + 1) % tips.length, 3500); }
  } else {
    if (queueInterval) { clearInterval(queueInterval); queueInterval = null; }
    if (tipInterval)   { clearInterval(tipInterval);   tipInterval = null; }
    queueSecs = 0; tipIndex = 0;
  }

  function formatQueueTime(s) {
    const m = Math.floor(s / 60);
    return m > 0 ? `${m}:${(s % 60).toString().padStart(2,'0')}` : `${s}s`;
  }

  function formatTimeAgo(dateStr) {
    const diff = Math.floor((Date.now() - new Date(dateStr).getTime()) / 1000);
    if (diff < 60)    return `${diff}s ago`;
    if (diff < 3600)  return `${Math.floor(diff / 60)}m ago`;
    if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`;
    return `${Math.floor(diff / 86400)}d ago`;
  }
</script>

<div class="rk-screen">

  {#each floatingLetters as fl}
    <span class="rk-float"
      style="left:{fl.x}%; top:{fl.y}%; font-size:{fl.s}rem; animation-duration:{fl.d}s; opacity:{fl.o}"
    >{fl.l}</span>
  {/each}

  <!-- Topbar -->
  <div class="rk-topbar">
    <button class="rk-back-btn" on:click={() => page.set('dashboard')}>‹</button>
    <div class="rk-eyebrow">
      <span class="rk-dot"></span>
      {$t('dashboard.ranked_title')}
    </div>
  </div>

  {#if $isWaiting}
    <!-- ── Searching ── -->
    <div class="rk-searching">
      <div class="rk-radar-wrap">
        <div class="rk-radar-ring r1"></div>
        <div class="rk-radar-ring r2"></div>
        <div class="rk-radar-ring r3"></div>
        <div class="rk-radar-avatar">
          {#if $profilePicUrl}
            <img src={$profilePicUrl} alt={$username} />
          {:else}
            <span>{$username.charAt(0).toUpperCase()}</span>
          {/if}
        </div>
      </div>
      {#if $rankedStats}
        <div class="rk-searching-tier">
          {$t(`ranked.tier_icon.${$rankedStats.tier.toLowerCase()}`)}
          {$t(`ranked.tier.${$rankedStats.tier.toLowerCase()}`)}
          <span class="rk-searching-rp">· {$rankedStats.rp} RP</span>
        </div>
      {/if}
      <p class="rk-searching-label">{$t('ranked.queue_title')}</p>
      <p class="rk-timer">{formatQueueTime(queueSecs)}</p>
      <p class="rk-tip">{$t(tips[tipIndex])}</p>
      <button class="rk-cancel" on:click={cancelRanked}>{$t('ranked.queue_cancel')}</button>
    </div>

  {:else}
    <!-- ── Pre-search ── -->
    <div class="rk-presearch">

      <!-- Identity -->
      <div class="rk-identity">
        <div class="rk-identity-avatar">
          {#if $profilePicUrl}
            <img src={$profilePicUrl} alt={$username} />
          {:else}
            <span>{$username.charAt(0).toUpperCase()}</span>
          {/if}
          <div class="rk-identity-dot"></div>
        </div>
        <span class="rk-identity-name">{$username}</span>
        {#if $activeTag}<span class="rk-identity-tag">{$activeTag}</span>{/if}
      </div>

      <!-- Stats strip -->
      {#if $rankedStats}
        <div class="rk-stats">
          <div class="rk-stat">
            <span class="rk-stat-val">{$t(`ranked.tier_icon.${$rankedStats.tier.toLowerCase()}`)} {$t(`ranked.tier.${$rankedStats.tier.toLowerCase()}`)}</span>
            <span class="rk-stat-label">{$t('ranked.tier_label')}</span>
          </div>
          <div class="rk-stat-sep"></div>
          <div class="rk-stat">
            <span class="rk-stat-val rk-stat-gold">{$rankedStats.rp}</span>
            <span class="rk-stat-label">RP</span>
          </div>
          <div class="rk-stat-sep"></div>
          <div class="rk-stat">
            <span class="rk-stat-val">{$rankedStats.wins}W <span style="opacity:0.4">/</span> {$rankedStats.losses}L</span>
            <span class="rk-stat-label">{$t('ranked.win_label')} / {$t('ranked.loss_label')}</span>
          </div>
          <div class="rk-stat-sep"></div>
          <div class="rk-stat">
            <span class="rk-stat-val rk-stat-gold">{$rankedStats.peakRp}</span>
            <span class="rk-stat-label">{$t('ranked.peak_label')}</span>
          </div>
        </div>
      {/if}

      <!-- CTA -->
      <button class="rk-play-btn" on:click={() => connectToRanked($username)}>
        <span class="rk-btn-pulse"></span>
        {$t('dashboard.ranked_btn')}
      </button>

      <!-- Match history -->
      {#if matchHistory.length > 0}
        <div class="rk-history">
          <p class="rk-history-title">{$t('ranked.history_label')}</p>
          {#each matchHistory as m}
            <div class="rk-match-row {m.won ? 'rk-win' : 'rk-loss'}">
              <span class="rk-match-badge">{m.won ? 'W' : 'L'}</span>
              <span class="rk-match-opp">{m.opponentName}</span>
              <span class="rk-match-score">{m.mySeriesWins}–{m.opponentSeriesWins}</span>
              <span class="rk-match-rp {m.rpChange >= 0 ? 'rp-gain' : 'rp-loss'}">{m.rpChange >= 0 ? '+' : ''}{m.rpChange}</span>
              <span class="rk-match-time">{formatTimeAgo(m.playedAt)}</span>
            </div>
          {/each}
        </div>
      {/if}

    </div>
  {/if}

</div>
