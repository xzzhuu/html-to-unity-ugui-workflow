"""Evidence publication must reject incomplete input and restore failed writes."""
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import record_unity_acceptance as recording


class AcceptancePublicationTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.root=Path(self.temp.name)

    def tearDown(self):self.temp.cleanup()

    def test_missing_visual_review_writes_nothing(self):
        with self.assertRaisesRegex(ValueError,'Inspect actual'):
            recording.record(self.root,self.root,self.root,self.root)
        self.assertFalse(list(self.root.iterdir()))

    def test_validation_failure_restores_existing_and_new_files(self):
        existing=self.root/'accepted.json';existing.write_bytes(b'previous acceptance')
        new=self.root/'new/capture.json'
        def reject():raise ValueError('stale dependencies')
        with self.assertRaisesRegex(ValueError,'stale dependencies'):
            recording.publish({existing:b'new acceptance',new:b'new evidence'},reject)
        self.assertEqual(existing.read_bytes(),b'previous acceptance')
        self.assertFalse(new.exists());self.assertFalse(new.parent.exists())

    def test_write_failure_restores_all_touched_files(self):
        first=self.root/'first.json';second=self.root/'second.json'
        first.write_bytes(b'one');second.write_bytes(b'two');original=recording.atomic_bytes;calls=[]
        def write(path,data):
            calls.append(path)
            if len(calls)==2:raise OSError('disk error')
            original(path,data)
        with patch.object(recording,'atomic_bytes',side_effect=write):
            with self.assertRaisesRegex(OSError,'disk error'):recording.publish({first:b'changed',second:b'changed'},lambda:None)
        self.assertEqual(first.read_bytes(),b'one');self.assertEqual(second.read_bytes(),b'two')

    def test_success_is_visible_only_after_validation(self):
        output=self.root/'evidence.json'
        recording.publish({output:b'complete'},lambda:self.assertEqual(output.read_bytes(),b'complete'))
        self.assertEqual(output.read_bytes(),b'complete')

    def test_duplicate_matrix_case_is_rejected(self):
        item=dict(panel='MainPanel',fixture='Default',viewport='1920x1080')
        with self.assertRaisesRegex(ValueError,'Duplicate'):
            recording.unique_index([item,item],lambda value:tuple(value.values()),'native case')

    def test_nonfinite_json_cannot_be_published(self):
        with self.assertRaises(ValueError):recording.json_bytes(dict(value=float('nan')))


if __name__=='__main__':unittest.main()
