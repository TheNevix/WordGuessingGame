<script>
  import { page } from '../stores.js';
  import { t } from '../i18n.js';

  const tabs = [
    { id: 'dashboard',   icon: '⚡', key: 'nav.play' },
    { id: 'challenges',  icon: '✦', key: 'nav.challenges_tab' },
    { id: 'leaderboard', icon: '▲', key: 'nav.leaderboard_tab' },
    { id: 'profile',     icon: '◉', key: 'nav.profile_tab' },
  ];
</script>

<nav class="bottom-nav">
  {#each tabs as tab}
    <button class="bnav-tab {$page === tab.id ? 'bnav-active' : ''}" on:click={() => page.set(tab.id)}>
      <span class="bnav-icon">{tab.icon}</span>
      <span class="bnav-label">{$t(tab.key)}</span>
    </button>
  {/each}
</nav>
