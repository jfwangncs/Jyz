import type { RouteRecordRaw } from 'vue-router';

import { $t } from '#/locales';

const routes: RouteRecordRaw[] = [
  {
    meta: {
      hideInMenu: true,
      title: $t('page.auth.profile'),
    },
    name: 'Profile',
    path: '/profile',
    component: () => import('#/views/_core/profile/index.vue'),
  },
];

export default routes;
