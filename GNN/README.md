# GNN Tabanlı Deprem Risk Puanlaması

Deprem sırasında kullanıcının durduğu noktanın güvenli, açık veya tehlikeli olduğunu
eğitilmiş bir **GraphSAGE** modeli belirler. BoxCollider trigger'ları artık puanlamada
kullanılmıyor, yalnızca eğitim etiketi olarak işe yarıyor.

## Akış

1. **Graf** (`Assets/_Scripts/GNN/GnnGraphBuilder.cs`): Ev zemini 0.25 m'lik hücrelere bölünür.
   Her yürünebilir hücre bir düğümdür. Arada engel olmayan 8 komşu hücre kenarla bağlanır.
2. **Düğüm özellikleri** (9 adet, yalnızca geometriden): üstteki örtüye uzaklık, örtünün eşya
   mı tavan mı olduğu, cama uzaklık, çevre açıklığı, yakındaki en yüksek eşya, yüksekte duran
   serbest eşya sayısı, sağlam alçak eşyaya (masa/koltuk) uzaklık, açık yön oranı.
3. **Etiket**: Mevcut `SafeArea` / `DangerArea` collider'larının içinde kalan düğümler.
4. **Eğitim** (`GNN/train_gnn.py`): 2 katmanlı GraphSAGE (mean aggregator) + softmax.
   Veri uzamsal bloklara göre bölünür, sınıf dengesizliği için ağırlıklı cross-entropy kullanılır.
5. **Çalışma zamanı** (`GnnRiskPredictor.cs`): Graf sahne açılınca birkaç kareye yayılarak kurulur.
   Deprem başladığında model **bir kez** çalışır. `CalculateScore` her 2 saniyede bir oyuncunun
   düğümünü sorgular ve cezayı risk skoruyla ölçekler.

## Kullanım

```bash
# 1) Unity: ExamScene'i aç → Tools > GNN > Eğitim Verisini Dışa Aktar (Açık Sahne)
#    → GNN/data/ExamScene_graph_{0..3}.json

# 2) Eğit
python3 -m venv GNN/.venv && source GNN/.venv/bin/activate
pip install -r GNN/requirements.txt
python3 GNN/train_gnn.py            # --onnx ile ONNX da üretir

# 3) Assets/Resources/GNN/gnn_weights.json oluşur → Unity otomatik yükler
```

Görsel kontrol için sahnedeki `GnnRiskPredictor` bileşenine sağ tık:
- **Grafı kur ve etiketleri göster**: eğitim etiketleri (yeşil / sarı / kırmızı)
- **Grafı kur ve modeli çalıştır**: model tahmini

Bu görüntüleri görmek için objenin seçili olması gerekir (gizmo).

Metrikler `GNN/report/metrics.json` dosyasına yazılır:
- 5-katlı uzamsal çapraz doğrulama (ortalama ± std)
- Sınıf bazlı P/R/F1 ve karışıklık matrisi
- 1-komşu toleranslı metrikler (etiket kutularının kenar hatalarını ayırmak için)

Oyuna giden model, erken durdurma katı dışındaki tüm verilerle eğitilir. Raporda çapraz doğrulama
ortalamasını kullanın. `--no-cv` verilirse klasik tek bölme (train/val/test) kullanılır.

> Grid ayarları (`GnnGraphSettings`) veya özellik kodu değişirse veriyi yeniden dışa aktarıp
> modeli yeniden eğitin. Model, özellik sırası uyuşmazsa yüklenmeyi reddeder.
