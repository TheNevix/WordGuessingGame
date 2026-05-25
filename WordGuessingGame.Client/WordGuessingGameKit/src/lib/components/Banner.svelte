<script>
  export let username = '';
  export let pfp = null;
  export let color = '#5b21b6';
  export let tags = [];
  export let isYou = false;
  export let isActive = false;
  export let size = 'md'; // 'sm' | 'md' | 'lg'
</script>

<div class="banner-card banner-{size} {isActive ? 'banner-active' : ''}" style="--banner-bg:{color}">
  <div class="banner-bg"></div>
  <div class="banner-content">
    <div class="banner-pfp">
      {#if pfp}
        <img src={pfp} alt={username} />
      {:else}
        {username ? username.charAt(0).toUpperCase() : '?'}
      {/if}
    </div>
    <div class="banner-info">
      <span class="banner-name">{username}</span>
      {#if tags.length > 0}
        <div class="banner-tags">
          {#each tags as tag}
            {@const tagName = typeof tag === 'string' ? tag : tag.name}
            {@const tagColor = typeof tag === 'string' ? null : tag.color}
            <span class="banner-tag" style={tagColor ? `border-color:${tagColor};color:${tagColor}` : ''}>{tagName}</span>
          {/each}
        </div>
      {/if}
    </div>
  </div>
</div>
