<script>
  import { onMount, onDestroy } from 'svelte';
  import { username, profilePicUrl, bannerColor, activeTag, isWaiting, rankedStats, page } from '../stores.js';
  import { connectToRanked, cancelRanked } from '../hub.js';
  import { fetchRankedStats, fetchRankedHistory } from '../api.js';
  import { t } from '../i18n.js';
  import Banner from '../components/Banner.svelte';

  let matchHistory = [];
  let queueSecs = 0;
  let queueInterval = null;

  onMount(async () => {
    try { const rs = await fetchRankedStats(); if (rs) rankedStats.set(rs); } catch { /* ignore */ }
    try { matchHistory = await fetchRankedHistory(); } catch { /* ignore */ }
  });

  onDestroy(() => {
    if (queueInterval) clearInterval(queueInterval);
  });

  $: if ($isWaiting) {
    if (!queueInterval) {
      queueSecs = 0;
      queueInterval = setInterval(() => queueSecs++, 1000);
    }
  } else {
    if (queueInterval) { clearInterval(queueInterval); queueInterval = null; }
    queueSecs = 0;
  }

  function formatQueueTime(s) {
    const m = Math.floor(s / 60);
    const sec = s % 60;
    return m > 0 ? `${m}:${sec.toString().padStart(2, '0')}` : `${s}s`;
  }

  function formatTimeAgo(dateStr) {
    const diff = Math.floor((Date.now() - new Date(dateStr).getTime()) / 1000);
    if (diff < 60) return `${diff}s ago`;
    if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
    if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`;
    return `${Math.floor(diff / 86400)}d ago`;
  }

  async function joinRanked() {
    await connectToRanked($username);
  }
</script>

<div class="auth-container">
  <div class="card">

    <div class="waiting-banner-wrap">
      <Banner
        username={$username}
        pfp={$profilePicUrl}
        color={$bannerColor}
        tags={$activeTag ? [$activeTag] : []}
        size="md"
      />
    </div>

    {#if $isWaiting}
      <p class="status-text">{$t('ranked.queue_title')}</p>
      {#if $rankedStats}
        <p class="subtitle">{$t('ranked.tier_label')}: <strong>{$t(`ranked.tier.${$rankedStats.tier.toLowerCase()}`)}</strong> · {$rankedStats.rp} RP</p>
      {/if}
      <p class="queue-timer">⏱ {formatQueueTime(queueSecs)}</p>
      <button class="btn-ghost btn-sm" on:click={cancelRanked}>{$t('ranked.queue_cancel')}</button>
    {:else}
      {#if $rankedStats}
        <div class="ranked-stats-row">
          <div class="ranked-stat">
            <span class="ranked-stat-val">{$t(`ranked.tier.${$rankedStats.tier.toLowerCase()}`)}</span>
            <span class="ranked-stat-label">{$t('ranked.tier_label')}</span>
          </div>
          <div class="ranked-stat">
            <span class="ranked-stat-val">{$rankedStats.rp} RP</span>
            <span class="ranked-stat-label">RP</span>
          </div>
          <div class="ranked-stat">
            <span class="ranked-stat-val">{$rankedStats.wins}W / {$rankedStats.losses}L</span>
            <span class="ranked-stat-label">{$t('ranked.win_label')} / {$t('ranked.loss_label')}</span>
          </div>
          <div class="ranked-stat">
            <span class="ranked-stat-val">{$rankedStats.peakRp} RP</span>
            <span class="ranked-stat-label">{$t('ranked.peak_label')}</span>
          </div>
        </div>
      {/if}
      <button class="btn-primary" style="margin-top:1rem" on:click={joinRanked}>{$t('dashboard.ranked_btn')}</button>
      <button class="btn-ghost btn-sm" style="margin-top:0.5rem" on:click={() => page.set('dashboard')}>← {$t('nav.back')}</button>

      {#if matchHistory.length > 0}
        <div class="match-history">
          <p class="match-history-title">{$t('ranked.history_label')}</p>
          {#each matchHistory as m}
            <div class="match-row {m.won ? 'match-win' : 'match-loss'}">
              <span class="match-result-badge">{m.won ? 'W' : 'L'}</span>
              <span class="match-opponent">{m.opponentName}</span>
              <span class="match-score">{m.mySeriesWins}–{m.opponentSeriesWins}</span>
              <span class="match-rp {m.rpChange >= 0 ? 'rp-gain' : 'rp-loss'}">{m.rpChange >= 0 ? '+' : ''}{m.rpChange}</span>
              <span class="match-time">{formatTimeAgo(m.playedAt)}</span>
            </div>
          {/each}
        </div>
      {/if}
    {/if}

  </div>
</div>
