using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Text;
using System.Web.Script.Serialization;

namespace PrimordialLauncher {
    public class SkinEntry {
        public string id { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public string description { get; set; }
        public string texture { get; set; }
        public string preview { get; set; }
        public string model { get; set; }

        public SkinEntry() {
            model = "classic";
        }
    }

    public class SkinCatalogManifest {
        public int version { get; set; }
        public List<SkinEntry> skins { get; set; }
    }

    public partial class LauncherForm : Form {
        // Skin Studio State & Controls
        private List<SkinEntry> allSkins = new List<SkinEntry>();
        private SkinEntry selectedSkin = null;
        private SkinEntry activeSkin = null;

        private FlowLayoutPanel pnlGallery;
        private Panel pnlPreviewCard;
        private PictureBox pbPreviewBig;
        private Label lblPreviewName;
        private Label lblPreviewCategory;
        private Label lblPreviewDesc;
        private Label lblPreviewDims;
        private Button btnApplySkin;
        private Label lblApplyFeedback;

        private TextBox txtMinotarUser;
        private Button btnMinotarClone;
        private Button btnBrowseSkin;
        private Label lblClonerStatus;

        private Button btnCatAll;
        private Button btnCatFunny;
        private Button btnCatCute;
        private Button btnCatFantasy;

        private string currentCategoryFilter = "all";
        private Image currentBigPreviewImage = null;
        private Image currentBadgeThumbnail = null;
        private Dictionary<string, Image> previewCache = new Dictionary<string, Image>();
        private List<Panel> galleryCards = new List<Panel>();

        public void BuildSkinsTab() {
            tabSkins = new Panel();
            tabSkins.Location = new Point(0, 0);
            tabSkins.Size = new Size(600, 580);
            tabSkins.BackColor = Color.FromArgb(14, 16, 23);
            pnlContent.Controls.Add(tabSkins);

            // Left Side - Header Title
            Label lblTitle = new Label();
            lblTitle.Text = "🎨 CHARACTER SKIN STUDIO";
            lblTitle.Font = new Font("Segoe UI", 12.5f, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(16, 185, 129); // Emerald
            lblTitle.Location = new Point(18, 12);
            lblTitle.AutoSize = true;
            tabSkins.Controls.Add(lblTitle);

            // 4 Category filter buttons
            btnCatAll = CreateCategoryFilterButton("🌟 All (16)", 18, 42, 75);
            btnCatFunny = CreateCategoryFilterButton("😂 Funny & Memes (4)", 97, 42, 97);
            btnCatCute = CreateCategoryFilterButton("💖 Cute & Cozy (6)", 198, 42, 85);
            btnCatFantasy = CreateCategoryFilterButton("⚔️ Fantasy (6)", 287, 42, 76);

            btnCatAll.Click += delegate { SetCategoryFilter("all"); };
            btnCatFunny.Click += delegate { SetCategoryFilter("funny"); };
            btnCatCute.Click += delegate { SetCategoryFilter("cute"); };
            btnCatFantasy.Click += delegate { SetCategoryFilter("fantasy"); };

            tabSkins.Controls.Add(btnCatAll);
            tabSkins.Controls.Add(btnCatFunny);
            tabSkins.Controls.Add(btnCatCute);
            tabSkins.Controls.Add(btnCatFantasy);

            // FlowLayoutPanel pnlGallery (size 345x368, AutoScroll true) holding 16 cards (106x135 each)
            pnlGallery = new FlowLayoutPanel();
            pnlGallery.Location = new Point(18, 76);
            pnlGallery.Size = new Size(345, 368);
            pnlGallery.AutoScroll = true;
            pnlGallery.BackColor = Color.FromArgb(14, 16, 23);
            pnlGallery.BorderStyle = BorderStyle.None;
            tabSkins.Controls.Add(pnlGallery);

            // Minotar Cloner Row
            txtMinotarUser = new TextBox();
            txtMinotarUser.Location = new Point(18, 452);
            txtMinotarUser.Size = new Size(230, 26);
            txtMinotarUser.BackColor = Color.FromArgb(28, 33, 48);
            txtMinotarUser.ForeColor = Color.White;
            txtMinotarUser.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            txtMinotarUser.BorderStyle = BorderStyle.FixedSingle;
            tabSkins.Controls.Add(txtMinotarUser);

            btnMinotarClone = new Button();
            btnMinotarClone.Text = "Clone Player";
            btnMinotarClone.Location = new Point(253, 451);
            btnMinotarClone.Size = new Size(110, 28);
            btnMinotarClone.FlatStyle = FlatStyle.Flat;
            btnMinotarClone.FlatAppearance.BorderSize = 0;
            btnMinotarClone.BackColor = Color.FromArgb(37, 99, 235);
            btnMinotarClone.ForeColor = Color.White;
            btnMinotarClone.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnMinotarClone.Cursor = Cursors.Hand;
            btnMinotarClone.Click += BtnMinotarClone_Click;
            tabSkins.Controls.Add(btnMinotarClone);

            // Local .png Button
            btnBrowseSkin = new Button();
            btnBrowseSkin.Text = "Browse .PNG...";
            btnBrowseSkin.Location = new Point(18, 485);
            btnBrowseSkin.Size = new Size(345, 28);
            btnBrowseSkin.FlatStyle = FlatStyle.Flat;
            btnBrowseSkin.FlatAppearance.BorderSize = 0;
            btnBrowseSkin.BackColor = Color.FromArgb(30, 41, 59);
            btnBrowseSkin.ForeColor = Color.FromArgb(226, 232, 240);
            btnBrowseSkin.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnBrowseSkin.Cursor = Cursors.Hand;
            btnBrowseSkin.Click += BtnBrowseSkin_Click;
            tabSkins.Controls.Add(btnBrowseSkin);

            // Cloner / Import status
            lblClonerStatus = new Label();
            lblClonerStatus.Location = new Point(18, 518);
            lblClonerStatus.Size = new Size(345, 20);
            lblClonerStatus.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblClonerStatus.ForeColor = Color.FromArgb(148, 163, 184);
            lblClonerStatus.TextAlign = ContentAlignment.MiddleLeft;
            tabSkins.Controls.Add(lblClonerStatus);

            // Right side: Preview card panel (208x540, BackColor 20, 23, 34)
            pnlPreviewCard = new Panel();
            pnlPreviewCard.Location = new Point(374, 14);
            pnlPreviewCard.Size = new Size(208, 540);
            pnlPreviewCard.BackColor = Color.FromArgb(20, 23, 34);
            pnlPreviewCard.Paint += delegate(object s, PaintEventArgs pe) {
                using (Pen pen = new Pen(Color.FromArgb(40, 46, 68), 1)) {
                    pe.Graphics.DrawRectangle(pen, 0, 0, pnlPreviewCard.Width - 1, pnlPreviewCard.Height - 1);
                }
            };
            tabSkins.Controls.Add(pnlPreviewCard);

            // 140x252 PictureBox (InterpolationMode.NearestNeighbor)
            pbPreviewBig = new PictureBox();
            pbPreviewBig.Location = new Point(34, 16);
            pbPreviewBig.Size = new Size(140, 252);
            pbPreviewBig.BackColor = Color.FromArgb(14, 16, 23);
            pbPreviewBig.Paint += delegate(object s, PaintEventArgs pe) {
                if (currentBigPreviewImage != null) {
                    pe.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    pe.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
                    pe.Graphics.DrawImage(currentBigPreviewImage, 0, 0, pbPreviewBig.Width, pbPreviewBig.Height);
                }
            };
            pnlPreviewCard.Controls.Add(pbPreviewBig);

            // Metadata Labels
            lblPreviewName = new Label();
            lblPreviewName.Location = new Point(10, 276);
            lblPreviewName.Size = new Size(188, 24);
            lblPreviewName.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblPreviewName.ForeColor = Color.White;
            lblPreviewName.TextAlign = ContentAlignment.MiddleCenter;
            pnlPreviewCard.Controls.Add(lblPreviewName);

            lblPreviewCategory = new Label();
            lblPreviewCategory.Location = new Point(10, 302);
            lblPreviewCategory.Size = new Size(188, 20);
            lblPreviewCategory.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblPreviewCategory.ForeColor = Color.FromArgb(52, 211, 153);
            lblPreviewCategory.TextAlign = ContentAlignment.MiddleCenter;
            pnlPreviewCard.Controls.Add(lblPreviewCategory);

            lblPreviewDesc = new Label();
            lblPreviewDesc.Location = new Point(10, 326);
            lblPreviewDesc.Size = new Size(188, 54);
            lblPreviewDesc.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblPreviewDesc.ForeColor = Color.FromArgb(203, 213, 225);
            lblPreviewDesc.TextAlign = ContentAlignment.TopCenter;
            pnlPreviewCard.Controls.Add(lblPreviewDesc);

            lblPreviewDims = new Label();
            lblPreviewDims.Location = new Point(10, 384);
            lblPreviewDims.Size = new Size(188, 20);
            lblPreviewDims.Font = new Font("Segoe UI", 8f, FontStyle.Regular);
            lblPreviewDims.ForeColor = Color.FromArgb(148, 163, 184);
            lblPreviewDims.TextAlign = ContentAlignment.MiddleCenter;
            pnlPreviewCard.Controls.Add(lblPreviewDims);

            // "✅ APPLY SKIN" button
            btnApplySkin = new Button();
            btnApplySkin.Text = "✅ APPLY SKIN";
            btnApplySkin.Location = new Point(15, 416);
            btnApplySkin.Size = new Size(178, 44);
            btnApplySkin.FlatStyle = FlatStyle.Flat;
            btnApplySkin.FlatAppearance.BorderSize = 0;
            btnApplySkin.BackColor = Color.FromArgb(16, 185, 129);
            btnApplySkin.ForeColor = Color.White;
            btnApplySkin.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnApplySkin.Cursor = Cursors.Hand;
            btnApplySkin.Click += BtnApplySkin_Click;
            pnlPreviewCard.Controls.Add(btnApplySkin);

            lblApplyFeedback = new Label();
            lblApplyFeedback.Location = new Point(10, 468);
            lblApplyFeedback.Size = new Size(188, 20);
            lblApplyFeedback.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblApplyFeedback.ForeColor = Color.FromArgb(52, 211, 153);
            lblApplyFeedback.TextAlign = ContentAlignment.MiddleCenter;
            lblApplyFeedback.Text = "";
            pnlPreviewCard.Controls.Add(lblApplyFeedback);

            // Load catalog and build gallery
            LoadSkinCatalog();
            UpdateCategoryButtonStyles();
            PopulateGallery();
        }

        public void BuildSkinBadgeControl() {
            pnlSkinBadge = new Panel();
            pnlSkinBadge.Location = new Point(25, 170);
            pnlSkinBadge.Size = new Size(550, 40);
            pnlSkinBadge.BackColor = Color.FromArgb(22, 26, 38);
            pnlSkinBadge.Paint += delegate(object s, PaintEventArgs pe) {
                using (Pen pen = new Pen(Color.FromArgb(40, 46, 68), 1)) {
                    pe.Graphics.DrawRectangle(pen, 0, 0, pnlSkinBadge.Width - 1, pnlSkinBadge.Height - 1);
                }
            };

            pbBadgeThumb = new PictureBox();
            pbBadgeThumb.Location = new Point(8, 4);
            pbBadgeThumb.Size = new Size(24, 32);
            pbBadgeThumb.BackColor = Color.Transparent;
            pbBadgeThumb.Paint += delegate(object s, PaintEventArgs pe) {
                if (currentBadgeThumbnail != null) {
                    pe.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    pe.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
                    pe.Graphics.DrawImage(currentBadgeThumbnail, 0, 0, pbBadgeThumb.Width, pbBadgeThumb.Height);
                }
            };
            pnlSkinBadge.Controls.Add(pbBadgeThumb);

            lblBadgeSkinName = new Label();
            lblBadgeSkinName.Location = new Point(40, 10);
            lblBadgeSkinName.Size = new Size(400, 20);
            lblBadgeSkinName.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblBadgeSkinName.ForeColor = Color.FromArgb(226, 232, 240);
            lblBadgeSkinName.TextAlign = ContentAlignment.MiddleLeft;
            pnlSkinBadge.Controls.Add(lblBadgeSkinName);

            Button btnBadgeChange = new Button();
            btnBadgeChange.Text = "Change 🎨";
            btnBadgeChange.Location = new Point(445, 6);
            btnBadgeChange.Size = new Size(95, 28);
            btnBadgeChange.FlatStyle = FlatStyle.Flat;
            btnBadgeChange.FlatAppearance.BorderSize = 0;
            btnBadgeChange.BackColor = Color.FromArgb(37, 99, 235);
            btnBadgeChange.ForeColor = Color.White;
            btnBadgeChange.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnBadgeChange.Cursor = Cursors.Hand;
            btnBadgeChange.Click += delegate {
                SwitchTab(tabSkins, btnNavSkins);
            };
            pnlSkinBadge.Controls.Add(btnBadgeChange);

            tabPlay.Controls.Add(pnlSkinBadge);
        }

        private Button CreateCategoryFilterButton(string text, int x, int y, int width) {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = new Point(x, y);
            btn.Size = new Size(width, 28);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.BackColor = Color.FromArgb(22, 26, 38);
            btn.ForeColor = Color.FromArgb(148, 163, 184);
            return btn;
        }

        private void SetCategoryFilter(string cat) {
            currentCategoryFilter = (cat ?? "all").Trim().ToLowerInvariant();
            UpdateCategoryButtonStyles();
            PopulateGallery();
        }

        private void UpdateCategoryButtonStyles() {
            if (btnCatAll == null) return;
            btnCatAll.BackColor = (currentCategoryFilter == "all") ? Color.FromArgb(30, 41, 59) : Color.FromArgb(22, 26, 38);
            btnCatAll.ForeColor = (currentCategoryFilter == "all") ? Color.FromArgb(52, 211, 153) : Color.FromArgb(148, 163, 184);

            btnCatFunny.BackColor = (currentCategoryFilter == "funny") ? Color.FromArgb(30, 41, 59) : Color.FromArgb(22, 26, 38);
            btnCatFunny.ForeColor = (currentCategoryFilter == "funny") ? Color.FromArgb(52, 211, 153) : Color.FromArgb(148, 163, 184);

            btnCatCute.BackColor = (currentCategoryFilter == "cute") ? Color.FromArgb(30, 41, 59) : Color.FromArgb(22, 26, 38);
            btnCatCute.ForeColor = (currentCategoryFilter == "cute") ? Color.FromArgb(52, 211, 153) : Color.FromArgb(148, 163, 184);

            btnCatFantasy.BackColor = (currentCategoryFilter == "fantasy") ? Color.FromArgb(30, 41, 59) : Color.FromArgb(22, 26, 38);
            btnCatFantasy.ForeColor = (currentCategoryFilter == "fantasy") ? Color.FromArgb(52, 211, 153) : Color.FromArgb(148, 163, 184);
        }

        private void LoadSkinCatalog() {
            allSkins.Clear();
            string skinsDir = GetSkinsDirectory();
            string catalogFile = Path.Combine(skinsDir, "catalog.json");

            if (File.Exists(catalogFile)) {
                try {
                    string json = File.ReadAllText(catalogFile, Encoding.UTF8);
                    var ser = new JavaScriptSerializer();
                    SkinCatalogManifest manifest = ser.Deserialize<SkinCatalogManifest>(json);
                    if (manifest != null && manifest.skins != null && manifest.skins.Count > 0) {
                        allSkins.AddRange(manifest.skins);
                    }
                } catch { }
            }

            if (allSkins.Count == 0) {
                // Guaranteed built-in catalog inventory
                allSkins.Add(new SkinEntry { id = "gentleman_duck", name = "Gentleman Duck", category = "funny", description = "Dapper yellow duck in a tuxedo & red bow tie", texture = "gentleman_duck.png", preview = "gentleman_duck-preview.png", model = "classic" });
                allSkins.Add(new SkinEntry { id = "cozy_frog", name = "Cozy Froggy", category = "cute", description = "Adorable smiling face in a pastel green frog hood", texture = "cozy_frog.png", preview = "cozy_frog-preview.png", model = "classic" });
                allSkins.Add(new SkinEntry { id = "toasty_bread", name = "Toasty Bread", category = "funny", description = "Happy smiling slice of warm buttered toast", texture = "toasty_bread.png", preview = "toasty_bread-preview.png", model = "classic" });
                allSkins.Add(new SkinEntry { id = "neko_kitty", name = "Neko Kitty", category = "cute", description = "Pastel cat ears hoodie with paw mittens", texture = "neko_kitty.png", preview = "neko_kitty-preview.png", model = "classic" });
                allSkins.Add(new SkinEntry { id = "derpy_banana", name = "Derpy Banana", category = "funny", description = "Goofy yellow banana suit with silly eyes", texture = "derpy_banana.png", preview = "derpy_banana-preview.png", model = "classic" });
                allSkins.Add(new SkinEntry { id = "baby_dino", name = "Baby Dino", category = "cute", description = "Lime green dinosaur pajama onesie", texture = "baby_dino.png", preview = "baby_dino-preview.png", model = "classic" });
                allSkins.Add(new SkinEntry { id = "gentleman_penguin", name = "Gentleman Penguin", category = "funny", description = "Chubby penguin with golden monocle & bowtie", texture = "gentleman_penguin.png", preview = "gentleman_penguin-preview.png", model = "classic" });
                allSkins.Add(new SkinEntry { id = "strawberry_cow", name = "Strawberry Cow", category = "cute", description = "Pastel pink cow with strawberry horns", texture = "strawberry_cow.png", preview = "strawberry_cow-preview.png", model = "classic" });
                allSkins.Add(new SkinEntry { id = "red_panda", name = "Red Panda", category = "cute", description = "Cute woodland red panda adventurer", texture = "red_panda.png", preview = "red_panda-preview.png", model = "classic" });
                allSkins.Add(new SkinEntry { id = "space_axolotl", name = "Space Axolotl", category = "cute", description = "Cosmic void creature in astronaut armor", texture = "space_axolotl.png", preview = "space_axolotl-preview.png", model = "classic" });
                allSkins.Add(new SkinEntry { id = "ember_mage", name = "Ember Mage", category = "fantasy", description = "Pyromancer wizard in crimson enchanted robes", texture = "ember_mage.png", preview = "ember_mage-preview.png", model = "classic" });
                allSkins.Add(new SkinEntry { id = "frost_mage", name = "Frost Mage", category = "fantasy", description = "Cryomancer spellcaster in chilled azure silk", texture = "frost_mage.png", preview = "frost_mage-preview.png", model = "classic" });
                allSkins.Add(new SkinEntry { id = "shadow_rogue", name = "Shadow Rogue", category = "fantasy", description = "Stealthy assassin draped in shadow mantle", texture = "shadow_rogue.png", preview = "shadow_rogue-preview.png", model = "classic" });
                allSkins.Add(new SkinEntry { id = "crystal_knight", name = "Crystal Knight", category = "fantasy", description = "Paladin wrapped in gleaming crystal armor", texture = "crystal_knight.png", preview = "crystal_knight-preview.png", model = "classic" });
                allSkins.Add(new SkinEntry { id = "forest_ranger", name = "Forest Ranger", category = "fantasy", description = "Elven scout in woodland leather & hood", texture = "forest_ranger.png", preview = "forest_ranger-preview.png", model = "classic" });
                allSkins.Add(new SkinEntry { id = "copper_robot", name = "Copper Robot", category = "fantasy", description = "Steampunk automaton with copper gears", texture = "copper_robot.png", preview = "copper_robot-preview.png", model = "classic" });
            }
        }

        private void PopulateGallery() {
            if (pnlGallery == null) return;
            pnlGallery.SuspendLayout();
            pnlGallery.Controls.Clear();
            galleryCards.Clear();

            List<SkinEntry> filtered = new List<SkinEntry>();
            foreach (SkinEntry s in allSkins) {
                if (currentCategoryFilter == "all" || string.IsNullOrEmpty(currentCategoryFilter)) {
                    filtered.Add(s);
                } else if (!string.IsNullOrEmpty(s.category) && s.category.ToLowerInvariant() == currentCategoryFilter) {
                    filtered.Add(s);
                }
            }

            foreach (SkinEntry skin in filtered) {
                Panel card = CreateSkinCard(skin);
                galleryCards.Add(card);
                pnlGallery.Controls.Add(card);
            }

            pnlGallery.ResumeLayout();

            if (selectedSkin != null && filtered.Contains(selectedSkin)) {
                SelectSkin(selectedSkin, false);
            } else if (filtered.Count > 0) {
                SelectSkin(filtered[0], false);
            }
        }

        private Panel CreateSkinCard(SkinEntry skin) {
            Panel card = new Panel();
            card.Size = new Size(106, 135);
            card.Margin = new Padding(3, 3, 3, 3);
            card.BackColor = (skin == selectedSkin) ? Color.FromArgb(30, 41, 59) : Color.FromArgb(22, 26, 38);
            card.Cursor = Cursors.Hand;
            card.Tag = skin;

            PictureBox pb = new PictureBox();
            pb.Location = new Point(13, 8);
            pb.Size = new Size(80, 88);
            pb.BackColor = Color.Transparent;
            pb.Cursor = Cursors.Hand;
            Image previewImg = GetSkinPreviewImage(skin);
            pb.Paint += delegate(object s, PaintEventArgs pe) {
                if (previewImg != null) {
                    pe.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    pe.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
                    pe.Graphics.DrawImage(previewImg, 0, 0, pb.Width, pb.Height);
                }
            };
            card.Controls.Add(pb);

            Label lblName = new Label();
            lblName.Location = new Point(2, 98);
            lblName.Size = new Size(102, 32);
            lblName.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblName.ForeColor = (skin == selectedSkin) ? Color.FromArgb(52, 211, 153) : Color.FromArgb(226, 232, 240);
            lblName.Text = skin.name;
            lblName.TextAlign = ContentAlignment.TopCenter;
            lblName.Cursor = Cursors.Hand;
            card.Controls.Add(lblName);

            Action clickAction = delegate {
                SelectSkin(skin, true);
            };
            card.Click += delegate { clickAction(); };
            pb.Click += delegate { clickAction(); };
            lblName.Click += delegate { clickAction(); };

            card.MouseEnter += delegate {
                if (skin != selectedSkin) card.BackColor = Color.FromArgb(28, 33, 48);
            };
            card.MouseLeave += delegate {
                if (skin != selectedSkin) card.BackColor = Color.FromArgb(22, 26, 38);
            };
            pb.MouseEnter += delegate {
                if (skin != selectedSkin) card.BackColor = Color.FromArgb(28, 33, 48);
            };
            pb.MouseLeave += delegate {
                if (skin != selectedSkin) card.BackColor = Color.FromArgb(22, 26, 38);
            };
            lblName.MouseEnter += delegate {
                if (skin != selectedSkin) card.BackColor = Color.FromArgb(28, 33, 48);
            };
            lblName.MouseLeave += delegate {
                if (skin != selectedSkin) card.BackColor = Color.FromArgb(22, 26, 38);
            };

            card.Paint += delegate(object s, PaintEventArgs pe) {
                if (skin == selectedSkin) {
                    using (Pen p = new Pen(Color.FromArgb(16, 185, 129), 2)) {
                        pe.Graphics.DrawRectangle(p, 1, 1, card.Width - 2, card.Height - 2);
                    }
                } else {
                    using (Pen p = new Pen(Color.FromArgb(35, 41, 60), 1)) {
                        pe.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                    }
                }
            };

            return card;
        }

        public void SelectSkin(SkinEntry skin, bool autoApply = true) {
            if (skin == null) return;
            selectedSkin = skin;

            currentBigPreviewImage = GetSkinPreviewImage(skin);
            if (pbPreviewBig != null) pbPreviewBig.Invalidate();

            if (lblPreviewName != null) lblPreviewName.Text = skin.name;
            if (lblPreviewCategory != null) lblPreviewCategory.Text = GetCategoryDisplayName(skin.category);
            if (lblPreviewDesc != null) lblPreviewDesc.Text = skin.description ?? "";
            if (lblPreviewDims != null) lblPreviewDims.Text = string.Format("Model: {0} | 64x64 PNG", skin.model ?? "classic");

            foreach (Panel card in galleryCards) {
                SkinEntry cardSkin = card.Tag as SkinEntry;
                if (cardSkin != null) {
                    bool isSel = (cardSkin.id == selectedSkin.id);
                    card.BackColor = isSel ? Color.FromArgb(30, 41, 59) : Color.FromArgb(22, 26, 38);
                    if (card.Controls.Count > 1 && card.Controls[1] is Label) {
                        card.Controls[1].ForeColor = isSel ? Color.FromArgb(52, 211, 153) : Color.FromArgb(226, 232, 240);
                    }
                    card.Invalidate();
                }
            }

            if (autoApply) {
                ApplySkin(skin);
                if (lblApplyFeedback != null) lblApplyFeedback.Text = "Active Skin Selected!";
            } else {
                if (lblApplyFeedback != null) lblApplyFeedback.Text = "";
            }
        }

        public void ApplySkin(SkinEntry skin) {
            if (skin == null) return;
            activeSkin = skin;
            string nick = (txtNickname != null && !string.IsNullOrEmpty(txtNickname.Text)) ? txtNickname.Text.Trim() : "primo";
            SaveSkinPreference(nick, skin.id);

            currentBadgeThumbnail = GetSkinPreviewImage(skin);
            if (pbBadgeThumb != null) pbBadgeThumb.Invalidate();

            if (lblBadgeSkinName != null) {
                lblBadgeSkinName.Text = string.Format("Selected Skin: {0} ({1})", skin.name, GetCategoryDisplayName(skin.category));
            }
            if (lblApplyFeedback != null) {
                lblApplyFeedback.Text = "Skin Applied Successfully!";
            }
        }

        private void BtnApplySkin_Click(object sender, EventArgs e) {
            if (selectedSkin != null) {
                ApplySkin(selectedSkin);
            }
        }

        public void InitActiveSkinFromPreferences() {
            string nick = (txtNickname != null && !string.IsNullOrEmpty(txtNickname.Text)) ? txtNickname.Text.Trim() : "primo";
            string savedId = LoadSkinPreference(nick);

            SkinEntry target = allSkins.Find(delegate(SkinEntry s) { return s.id == savedId; });
            if (target == null && allSkins.Count > 0) {
                target = allSkins.Find(delegate(SkinEntry s) { return s.id == "cozy_frog"; }) ?? allSkins[0];
            }
            if (target != null) {
                SelectSkin(target, true);
            }
        }

        public Image LoadImageWithoutLocking(string path) {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try {
                byte[] bytes = File.ReadAllBytes(path);
                using (MemoryStream ms = new MemoryStream(bytes))
                using (Image img = Image.FromStream(ms)) {
                    return new Bitmap(img);
                }
            } catch {
                return null;
            }
        }

        public void SaveSkinPreference(string nickname, string skinId) {
            if (string.IsNullOrEmpty(skinId)) return;
            try {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string prefDir = Path.Combine(localAppData, "PrimordialAdventures");
                Directory.CreateDirectory(prefDir);

                // Multi-profile persistence in skin_preferences.json
                string jsonFile = Path.Combine(prefDir, "skin_preferences.json");
                Dictionary<string, string> dict = new Dictionary<string, string>();
                if (File.Exists(jsonFile)) {
                    try {
                        var ser = new JavaScriptSerializer();
                        dict = ser.Deserialize<Dictionary<string, string>>(File.ReadAllText(jsonFile, Encoding.UTF8)) ?? new Dictionary<string, string>();
                    } catch { }
                }

                string key = System.Text.RegularExpressions.Regex.IsMatch(nickname ?? "", @"^[A-Za-z0-9_]{3,16}$") ? nickname.Trim() : "default";
                dict[key] = skinId;
                var serializer = new JavaScriptSerializer();
                File.WriteAllText(jsonFile, serializer.Serialize(dict), Encoding.UTF8);

                // Also persist {nickname}.skin
                string nickFile = Path.Combine(prefDir, key + ".skin");
                File.WriteAllText(nickFile, skinId, Encoding.UTF8);
            } catch { }
        }

        public string LoadSkinPreference(string nickname) {
            try {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string prefDir = Path.Combine(localAppData, "PrimordialAdventures");
                string key = System.Text.RegularExpressions.Regex.IsMatch(nickname ?? "", @"^[A-Za-z0-9_]{3,16}$") ? nickname.Trim() : "default";

                string nickFile = Path.Combine(prefDir, key + ".skin");
                if (File.Exists(nickFile)) {
                    string s = File.ReadAllText(nickFile, Encoding.UTF8).Trim();
                    if (!string.IsNullOrEmpty(s)) return s;
                }

                string jsonFile = Path.Combine(prefDir, "skin_preferences.json");
                if (File.Exists(jsonFile)) {
                    var ser = new JavaScriptSerializer();
                    var dict = ser.Deserialize<Dictionary<string, string>>(File.ReadAllText(jsonFile, Encoding.UTF8));
                    if (dict != null) {
                        if (dict.ContainsKey(key) && !string.IsNullOrEmpty(dict[key])) return dict[key];
                        if (dict.ContainsKey("default") && !string.IsNullOrEmpty(dict["default"])) return dict["default"];
                    }
                }
            } catch { }
            return "cozy_frog";
        }

        public string GetActiveSkinRelativePath() {
            if (activeSkin == null) return "skins/cozy_frog.png";
            if (!string.IsNullOrEmpty(activeSkin.texture)) {
                if (activeSkin.texture.StartsWith("skins/") || activeSkin.texture.StartsWith("skins\\")) {
                    return activeSkin.texture.Replace('\\', '/');
                }
                if (Path.IsPathRooted(activeSkin.texture)) {
                    return activeSkin.texture.Replace('\\', '/');
                }
                return "skins/" + Path.GetFileName(activeSkin.texture);
            }
            return "skins/cozy_frog.png";
        }

        private string GetCategoryDisplayName(string cat) {
            if (string.IsNullOrEmpty(cat)) return "All";
            string lower = cat.ToLowerInvariant();
            if (lower == "funny") return "Funny & Memes";
            if (lower == "cute") return "Cute & Cozy";
            if (lower == "fantasy") return "Fantasy";
            if (lower == "custom") return "Custom";
            return cat;
        }

        private string GetSkinsDirectory() {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates = new string[] {
                Path.Combine(baseDir, "pack", "skins"),
                Path.Combine(baseDir, "skins"),
                Path.Combine(baseDir, "portable-build", "Primordial-Adventures-Portable", "pack", "skins"),
                Path.Combine(baseDir, "portable-build", "Primordial-Adventures-Portable", "skins"),
                !string.IsNullOrEmpty(baseInstallPath) && baseInstallPath != "GLOBAL" ? Path.Combine(baseInstallPath, "pack", "skins") : null,
                !string.IsNullOrEmpty(baseInstallPath) && baseInstallPath != "GLOBAL" ? Path.Combine(baseInstallPath, "skins") : null
            };
            foreach (string dir in candidates) {
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir) && File.Exists(Path.Combine(dir, "catalog.json"))) {
                    return dir;
                }
            }
            return Path.Combine(baseDir, "pack", "skins");
        }

        public Bitmap RenderSkinComposite(Image texture) {
            if (texture == null) return null;
            Bitmap bmp = new Bitmap(80, 144, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp)) {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.SmoothingMode = SmoothingMode.None;
                g.Clear(Color.Transparent);

                bool is64x64 = texture.Height >= 64;

                // Head (dst: 20, 0, 40, 40; src: 8, 8, 8, 8)
                g.DrawImage(texture, new Rectangle(20, 0, 40, 40), new Rectangle(8, 8, 8, 8), GraphicsUnit.Pixel);
                // Head overlay / hat (src: 40, 8, 8, 8)
                g.DrawImage(texture, new Rectangle(20, 0, 40, 40), new Rectangle(40, 8, 8, 8), GraphicsUnit.Pixel);

                // Body (dst: 20, 40, 40, 60; src: 20, 20, 8, 12)
                g.DrawImage(texture, new Rectangle(20, 40, 40, 60), new Rectangle(20, 20, 8, 12), GraphicsUnit.Pixel);

                // Right Arm (dst: 0, 40, 20, 60; src: 44, 20, 4, 12)
                g.DrawImage(texture, new Rectangle(0, 40, 20, 60), new Rectangle(44, 20, 4, 12), GraphicsUnit.Pixel);

                // Left Arm (dst: 60, 40, 20, 60)
                if (is64x64) {
                    g.DrawImage(texture, new Rectangle(60, 40, 20, 60), new Rectangle(36, 52, 4, 12), GraphicsUnit.Pixel);
                } else {
                    using (Bitmap arm = new Bitmap(4, 12, PixelFormat.Format32bppArgb)) {
                        using (Graphics ag = Graphics.FromImage(arm)) {
                            ag.DrawImage(texture, new Rectangle(0, 0, 4, 12), new Rectangle(44, 20, 4, 12), GraphicsUnit.Pixel);
                        }
                        arm.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        g.DrawImage(arm, new Rectangle(60, 40, 20, 60), new Rectangle(0, 0, 4, 12), GraphicsUnit.Pixel);
                    }
                }

                // Right Leg (dst: 20, 100, 20, 44; src: 4, 20, 4, 12)
                g.DrawImage(texture, new Rectangle(20, 100, 20, 44), new Rectangle(4, 20, 4, 12), GraphicsUnit.Pixel);

                // Left Leg (dst: 40, 100, 20, 44)
                if (is64x64) {
                    g.DrawImage(texture, new Rectangle(40, 100, 20, 44), new Rectangle(20, 52, 4, 12), GraphicsUnit.Pixel);
                } else {
                    using (Bitmap leg = new Bitmap(4, 12, PixelFormat.Format32bppArgb)) {
                        using (Graphics lg = Graphics.FromImage(leg)) {
                            lg.DrawImage(texture, new Rectangle(0, 0, 4, 12), new Rectangle(4, 20, 4, 12), GraphicsUnit.Pixel);
                        }
                        leg.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        g.DrawImage(leg, new Rectangle(40, 100, 20, 44), new Rectangle(0, 0, 4, 12), GraphicsUnit.Pixel);
                    }
                }
            }
            return bmp;
        }

        public Image GetSkinPreviewImage(SkinEntry skin) {
            if (skin == null) return null;
            if (previewCache.ContainsKey(skin.id)) {
                return previewCache[skin.id];
            }

            string skinsDir = GetSkinsDirectory();
            Image img = null;
            if (!string.IsNullOrEmpty(skin.preview)) {
                string prevPath = Path.IsPathRooted(skin.preview) ? skin.preview : Path.Combine(skinsDir, skin.preview);
                if (File.Exists(prevPath)) {
                    img = LoadImageWithoutLocking(prevPath);
                }
            }

            if (img == null && !string.IsNullOrEmpty(skin.texture)) {
                string texPath = Path.IsPathRooted(skin.texture) ? skin.texture : Path.Combine(skinsDir, skin.texture);
                if (File.Exists(texPath)) {
                    using (Image tex = LoadImageWithoutLocking(texPath)) {
                        if (tex != null) {
                            img = RenderSkinComposite(tex);
                        }
                    }
                }
            }

            if (img != null) {
                previewCache[skin.id] = img;
            }
            return img;
        }

        private void BtnMinotarClone_Click(object sender, EventArgs e) {
            string username = txtMinotarUser.Text != null ? txtMinotarUser.Text.Trim() : "";
            if (string.IsNullOrEmpty(username)) {
                lblClonerStatus.Text = "Enter a valid player username.";
                lblClonerStatus.ForeColor = Color.FromArgb(239, 68, 68);
                return;
            }
            lblClonerStatus.Text = "Cloning " + username + "...";
            lblClonerStatus.ForeColor = Color.FromArgb(148, 163, 184);
            btnMinotarClone.Enabled = false;

            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    string url = "https://minotar.net/skin/" + Uri.EscapeDataString(username);
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                    req.Timeout = 3000;
                    req.ReadWriteTimeout = 3000;
                    req.UserAgent = "PrimordialLauncher/1.2.0";

                    byte[] data;
                    using (WebResponse resp = req.GetResponse())
                    using (Stream s = resp.GetResponseStream())
                    using (MemoryStream ms = new MemoryStream()) {
                        byte[] buffer = new byte[4096];
                        int read;
                        while ((read = s.Read(buffer, 0, buffer.Length)) > 0) {
                            ms.Write(buffer, 0, read);
                        }
                        data = ms.ToArray();
                    }

                    // Validate image dimensions
                    using (MemoryStream ms = new MemoryStream(data))
                    using (Image img = Image.FromStream(ms)) {
                        int w = img.Width;
                        int h = img.Height;
                        if (!((w == 64 && h == 64) || (w == 64 && h == 32))) {
                            throw new InvalidOperationException("Invalid skin dimensions: " + w + "x" + h);
                        }
                    }

                    // Save to local cache
                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string skinsDir = Path.Combine(localAppData, "PrimordialAdventures", "skins");
                    Directory.CreateDirectory(skinsDir);
                    string texFile = Path.Combine(skinsDir, "cloned_" + username + ".png");
                    File.WriteAllBytes(texFile, data);

                    SkinEntry cloned = new SkinEntry {
                        id = "cloned_" + username,
                        name = username + " (Cloned)",
                        category = "custom",
                        description = "Cloned Minotar skin from " + username,
                        texture = texFile,
                        model = "classic"
                    };

                    this.BeginInvoke(new Action(delegate {
                        btnMinotarClone.Enabled = true;
                        lblClonerStatus.Text = "Cloned " + username + " successfully!";
                        lblClonerStatus.ForeColor = Color.FromArgb(52, 211, 153);
                        allSkins.Add(cloned);
                        SelectSkin(cloned, true);
                    }));
                } catch (Exception) {
                    this.BeginInvoke(new Action(delegate {
                        btnMinotarClone.Enabled = true;
                        lblClonerStatus.Text = "Cloning failed (offline or invalid user).";
                        lblClonerStatus.ForeColor = Color.FromArgb(239, 68, 68);
                    }));
                }
            });
        }

        private void BtnBrowseSkin_Click(object sender, EventArgs e) {
            using (OpenFileDialog ofd = new OpenFileDialog()) {
                ofd.Title = "Select Minecraft Skin (.png)";
                ofd.Filter = "PNG Skin Files (*.png)|*.png|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK) {
                    try {
                        byte[] bytes = File.ReadAllBytes(ofd.FileName);
                        using (MemoryStream ms = new MemoryStream(bytes))
                        using (Image img = Image.FromStream(ms)) {
                            int w = img.Width;
                            int h = img.Height;
                            if (!((w == 64 && h == 64) || (w == 64 && h == 32))) {
                                MessageBox.Show("Skin texture must be a 64x64 or 64x32 PNG file.\nSelected image is " + w + "x" + h + ".",
                                    "Invalid Skin Dimensions", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }

                        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        string skinsDir = Path.Combine(localAppData, "PrimordialAdventures", "skins");
                        Directory.CreateDirectory(skinsDir);
                        string customName = Path.GetFileNameWithoutExtension(ofd.FileName);
                        string targetPath = Path.Combine(skinsDir, "custom_" + Path.GetFileName(ofd.FileName));
                        File.Copy(ofd.FileName, targetPath, true);

                        SkinEntry customSkin = new SkinEntry {
                            id = "custom_" + customName,
                            name = customName,
                            category = "custom",
                            description = "Local custom skin: " + Path.GetFileName(ofd.FileName),
                            texture = targetPath,
                            model = "classic"
                        };

                        lblClonerStatus.Text = "Loaded " + customName + " successfully!";
                        lblClonerStatus.ForeColor = Color.FromArgb(52, 211, 153);
                        allSkins.Add(customSkin);
                        SelectSkin(customSkin, true);
                    } catch (Exception ex) {
                        MessageBox.Show("Failed to load skin image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
