<script>
  import { onMount } from 'svelte';
  import { username, leaderboard, rankedStats } from '../stores.js';
  import { fetchLeaderboard, fetchRankedStats } from '../api.js';
  import { t } from '../i18n.js';

  onMount(async () => {
    try { const lb = await fetchLeaderboard(); leaderboard.set(lb); } catch { /* ignore */ }
    try { const rs = await fetchRankedStats(); if (rs) rankedStats.set(rs); } catch { /* ignore */ }
  });
</script>

<div class="tab-screen">

  <div class="tab-header">
    <h1 class="tab-header-title">{$t('nav.leaderboard_tab')}</h1>
    <span class="tab-header-season">{$rankedStats?.seasonName ?? 'Season 1'}</span>
  </div>

  <div class="tab-content">
    {#if $leaderboard.length === 0}
      <p class="tab-empty">{$t('leaderboard.empty')}</p>
    {:else}
      <div class="lb-list">
        {#each $leaderboard as entry}
          <div class="lb-row {entry.username === $username ? 'lb-row-me' : ''} {entry.rank <= 3 ? `lb-top-${entry.rank}` : ''}">
            <span class="lb-rank">
              {#if entry.rank === 1}🥇{:else if entry.rank === 2}🥈{:else if entry.rank === 3}🥉{:else}#{entry.rank}{/if}
            </span>
            <div class="lb-avatar">
              {#if entry.profilePictureUrl}
                <img src={entry.profilePictureUrl} alt={entry.username} />
              {:else}
                {entry.username.charAt(0).toUpperCase()}
              {/if}
            </div>
            <span class="lb-name">{entry.username}</span>
            <span class="lb-tier">{$t(`ranked.tier_icon.${entry.tier.toLowerCase()}`)}</span>
            <span class="lb-rp">{entry.rp} RP</span>
          </div>
        {/each}
      </div>
    {/if}
  </div>

</div>
